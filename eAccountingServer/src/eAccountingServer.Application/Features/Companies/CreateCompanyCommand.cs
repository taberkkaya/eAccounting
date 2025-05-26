using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.ValueObjects;
using GenericRepository;
using Mapster;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.Companies;
public sealed record CreateCompanyCommand(
    string Name,
    string Address,
    Database Database,
    string TaxDepartment,
    string TaxNumber
    ) : IRequest<Result<string>>;

public sealed class CreateCompanyCommandHandler(
    ICompanyRepository companyRepository,
    ICacheService cacheService,
    IUnitOfWork unitOfWork
    ) : IRequestHandler<CreateCompanyCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        bool isTaxNumberExist = await companyRepository.AnyAsync(p => p.TaxNumber == request.TaxNumber, cancellationToken);

        if(isTaxNumberExist)
            return Result<string>.Failure("Tax number already exists.");

        Company company = request.Adapt<Company>();
        await companyRepository.AddAsync(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        cacheService.Remove("companies");

        return "Company created successfully.";
    }
}
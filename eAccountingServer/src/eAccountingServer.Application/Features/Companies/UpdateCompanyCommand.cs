using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.ValueObjects;
using GenericRepository;
using Mapster;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.Companies;
public sealed record UpdateCompanyCommand(
   Guid Id,
   string Name,
   string Address,
   Database Database,
   string TaxDepartment,
   string TaxNumber
    ) : IRequest<Result<string>>;

internal sealed class UpdateCompanyCommandHandler(
    ICompanyRepository companyRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService
    ) : IRequestHandler<UpdateCompanyCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        Company? company = await companyRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (company is null)
            return Result<string>.Failure("Company not found.");

        bool isTaxNumberExist = await companyRepository.AnyAsync(p => p.TaxNumber == request.TaxNumber && p.Id != request.Id, cancellationToken);

        if (isTaxNumberExist)
            return Result<string>.Failure("Tax number already exists.");

        company = request.Adapt(company);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        cacheService.Remove("companies");

        return "Company updated successfully.";
    }
}
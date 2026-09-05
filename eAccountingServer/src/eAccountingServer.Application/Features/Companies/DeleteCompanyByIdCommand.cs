using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using GenericRepository;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.Companies;
public sealed record DeleteCompanyByIdCommand(Guid Id) : IRequest<Result<string>>;

internal sealed class DeleteCompanyByIdCommandHandler(
    ICompanyRepository companyRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService
    ) : IRequestHandler<DeleteCompanyByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteCompanyByIdCommand request, CancellationToken cancellationToken)
    {
        Company? company = await companyRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (company is null)
            return Result<string>.Failure("Firma bulunamadı.");

        company.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        cacheService.Remove("companies");
        return "Firma silindi.";
    }
}

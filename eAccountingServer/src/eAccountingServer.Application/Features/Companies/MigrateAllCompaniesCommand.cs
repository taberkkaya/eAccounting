using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Companies;
public sealed record MigrateAllCompaniesCommand() : IRequest<Result<string>>;

internal sealed class MigrateAllCompaniesCommandHandler(
    ICompanyRepository companyRepository,
    ICompanyService companyService
    ) : IRequestHandler<MigrateAllCompaniesCommand, Result<string>>
{
    public async Task<Result<string>> Handle(MigrateAllCompaniesCommand request, CancellationToken cancellationToken)
    {
        List<Company> companies = await companyRepository.GetAll().ToListAsync(cancellationToken);
        companyService.MigrateAllCompanies(companies);

        return "Companies migration successfully.";
    }
}

using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Companies;
public sealed record GetAllCompaniesQuery() : IRequest<Result<List<Company>>>;

internal sealed class GetAllCompaniesQueryHandler(
    ICompanyRepository companyRepository,
    ICacheService cacheService,
    IDemoContext demoContext
    ) : IRequestHandler<GetAllCompaniesQuery, Result<List<Company>>>
{
    public async Task<Result<List<Company>>> Handle(GetAllCompaniesQuery request, CancellationToken cancellationToken)
    {
        // A demo visitor may only see the sandbox tenant they were given; the other
        // sandboxes belong to whoever is browsing at the same time.
        if (demoContext.IsDemoRequest)
        {
            List<Company> ownCompany = await companyRepository
                .Where(p => p.Id == demoContext.CompanyId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // The visitor has no business knowing which server or database backs their
            // sandbox, so the connection details never leave the API.
            foreach (Company company in ownCompany)
                company.Database = new Database(string.Empty, string.Empty, string.Empty, string.Empty);

            return ownCompany;
        }

        List<Company>? companies;

        companies = cacheService.Get<List<Company>>("companies");

        if(companies is null)
        {
            companies = await companyRepository
                .GetAll()
                .OrderBy(p => p.Name)
                .ToListAsync();

            cacheService.Set<List<Company>>("companies", companies);
        }

        return companies;
    }
}

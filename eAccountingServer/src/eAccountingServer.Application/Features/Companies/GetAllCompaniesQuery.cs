using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Companies;
public sealed record GetAllCompaniesQuery() : IRequest<Result<List<Company>>>;

internal sealed class GetAllCompaniesQueryHandler(
    ICompanyRepository companyRepository,
    ICacheService cacheService
    ) : IRequestHandler<GetAllCompaniesQuery, Result<List<Company>>>
{
    public async Task<Result<List<Company>>> Handle(GetAllCompaniesQuery request, CancellationToken cancellationToken)
    {

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

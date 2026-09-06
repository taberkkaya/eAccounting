using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Companies;

/// <summary>
/// Bir firmanın belgelerde görünen bilgileri.
///
/// <see cref="GetAllCompaniesQuery"/> firma nesnesinin tamamını döndürüyor;
/// faturanın başlığına ünvan yazmak için oradan geçmek, veritabanı bağlantı
/// bilgilerini de istemciye taşımak olurdu. Bu sorgu yalnızca kâğıda basılan
/// alanları veriyor.
/// </summary>
public sealed record CompanyProfileDto(
    Guid Id,
    string Name,
    string? TaxNumber,
    string? TaxDepartment,
    string? Address);

public sealed record GetCompanyProfileQuery(Guid Id) : IRequest<Result<CompanyProfileDto>>;

internal sealed class GetCompanyProfileQueryHandler(
    ICompanyRepository companyRepository
    ) : IRequestHandler<GetCompanyProfileQuery, Result<CompanyProfileDto>>
{
    public async Task<Result<CompanyProfileDto>> Handle(
        GetCompanyProfileQuery request, CancellationToken cancellationToken)
    {
        Company? company = await companyRepository
            .Where(p => p.Id == request.Id)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
            return Result<CompanyProfileDto>.Failure("Firma bulunamadı.");

        return new CompanyProfileDto(
            company.Id,
            company.Name,
            string.IsNullOrWhiteSpace(company.TaxNumber) ? null : company.TaxNumber,
            string.IsNullOrWhiteSpace(company.TaxDepartment) ? null : company.TaxDepartment,
            string.IsNullOrWhiteSpace(company.Address) ? null : company.Address);
    }
}

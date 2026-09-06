using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.Reporting;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Contacts;

/// <summary>
/// Cari ekstresini Excel ya da PDF olarak indirir.
///
/// Ön muhasebede bu dosya mutabakat belgesidir: karşı tarafa gönderilip "sizde
/// de böyle mi görünüyor" diye sorulur. Bu yüzden devreden bakiye ve dönem
/// toplamları dosyada da görünüyor, ekrandakiyle aynı rakamlarla.
/// </summary>
public sealed record ExportContactStatementQuery(
    Guid ContactId,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    ReportFormat Format = ReportFormat.Excel) : IRequest<Result<ReportFile>>;

internal sealed class ExportContactStatementQueryHandler(
    IContactRepository contactRepository,
    IContactTransactionRepository contactTransactionRepository,
    IStatementReportBuilder reportBuilder
    ) : IRequestHandler<ExportContactStatementQuery, Result<ReportFile>>
{
    public async Task<Result<ReportFile>> Handle(
        ExportContactStatementQuery request, CancellationToken cancellationToken)
    {
        Contact? contact = await contactRepository
            .Where(p => p.Id == request.ContactId).FirstOrDefaultAsync(cancellationToken);

        if (contact is null)
            return Result<ReportFile>.Failure("Cari bulunamadı.");

        List<ContactTransaction> all = await contactTransactionRepository
            .Where(p => p.ContactId == request.ContactId)
            .OrderBy(p => p.Date).ThenBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        DateOnly start = request.StartDate
            ?? all.Select(p => p.Date).DefaultIfEmpty(DateOnly.FromDateTime(DateTime.Today)).Min();

        DateOnly end = request.EndDate ?? DateOnly.FromDateTime(DateTime.Today);

        decimal opening = all
            .Where(p => p.Date < start)
            .Sum(p => p.DebitAmount - p.CreditAmount);

        List<StatementLine> lines = all
            .Where(p => p.Date >= start && p.Date <= end)
            .Select(p => new StatementLine(
                p.Date,
                p.Description,
                p.DebitAmount,
                p.CreditAmount,
                IsTransfer: false))
            .ToList();

        Statement statement = new(
            "Cari",
            contact.Name,
            contact.CurrencyType.Name,
            StatementSymbols.For(contact.CurrencyType.Name),
            start,
            end,
            lines)
        {
            OpeningBalance = opening,
            Labels = StatementLabels.Contact
        };

        return reportBuilder.Build(statement, request.Format);
    }
}

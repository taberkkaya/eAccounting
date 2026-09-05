using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.Reporting;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.CashRegisterDetails;

public sealed record ExportCashRegisterDetailsQuery(
    Guid CashRegisterId,
    DateOnly StartDate,
    DateOnly EndDate,
    ReportFormat Format
    ) : IRequest<Result<ReportFile>>;

internal sealed class ExportCashRegisterDetailsQueryHandler(
    ICashRegisterRepository cashRegisterRepository,
    IStatementReportBuilder reportBuilder
    ) : IRequestHandler<ExportCashRegisterDetailsQuery, Result<ReportFile>>
{
    public async Task<Result<ReportFile>> Handle(ExportCashRegisterDetailsQuery request, CancellationToken cancellationToken)
    {
        // Ekrandaki listeyle aynı aralık ve sıra; indirilen dosya görülenden
        // farklı bir şey göstermemeli.
        CashRegister? cashRegister = await cashRegisterRepository
            .Where(p => p.Id == request.CashRegisterId)
            .Include(p => p.Details!
                .Where(detail =>
                    detail.Date >= request.StartDate
                    && detail.Date <= request.EndDate))
            .FirstOrDefaultAsync(cancellationToken);

        if (cashRegister is null)
            return Result<ReportFile>.Failure("Kasa bulunamadı.");

        Statement statement = new(
            "Kasa",
            cashRegister.Name,
            cashRegister.CurrencyType.Name,
            StatementSymbols.For(cashRegister.CurrencyType.Name),
            request.StartDate,
            request.EndDate,
            (cashRegister.Details ?? [])
                .OrderBy(detail => detail.Date)
                .ThenBy(detail => detail.CreatedAt)
                .Select(detail => new StatementLine(
                    detail.Date,
                    detail.Description,
                    detail.DepositAmount,
                    detail.WithdrawalAmount,
                    detail.CashRegisterDetailId is not null))
                .ToList());

        return reportBuilder.Build(statement, request.Format);
    }
}

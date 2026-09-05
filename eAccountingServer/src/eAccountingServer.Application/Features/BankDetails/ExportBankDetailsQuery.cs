using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.Reporting;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.BankDetails;

public sealed record ExportBankDetailsQuery(
    Guid BankId,
    DateOnly StartDate,
    DateOnly EndDate,
    ReportFormat Format
    ) : IRequest<Result<ReportFile>>;

internal sealed class ExportBankDetailsQueryHandler(
    IBankRepository bankRepository,
    IStatementReportBuilder reportBuilder
    ) : IRequestHandler<ExportBankDetailsQuery, Result<ReportFile>>
{
    public async Task<Result<ReportFile>> Handle(ExportBankDetailsQuery request, CancellationToken cancellationToken)
    {
        Bank? bank = await bankRepository
            .Where(p => p.Id == request.BankId)
            .Include(p => p.Details!
                .Where(detail =>
                    detail.Date >= request.StartDate
                    && detail.Date <= request.EndDate))
            .FirstOrDefaultAsync(cancellationToken);

        if (bank is null)
            return Result<ReportFile>.Failure("Banka bulunamadı.");

        Statement statement = new(
            "Banka",
            bank.Name,
            bank.CurrencyType.Name,
            StatementSymbols.For(bank.CurrencyType.Name),
            request.StartDate,
            request.EndDate,
            (bank.Details ?? [])
                .OrderBy(detail => detail.Date)
                .ThenBy(detail => detail.CreatedAt)
                .Select(detail => new StatementLine(
                    detail.Date,
                    detail.Description,
                    detail.DepositAmount,
                    detail.WithdrawalAmount,
                    detail.BankDetailId is not null))
                .ToList());

        return reportBuilder.Build(statement, request.Format);
    }
}

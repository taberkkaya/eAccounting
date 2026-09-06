using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Reporting;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.Movements;

/// <summary>
/// Ekrandaki hareket listesini Excel ya da PDF olarak indirir. Filtreler
/// <see cref="GetMovementsQuery"/> ile birebir aynı; dosya, listede görülenden
/// başka bir şey göstermemeli.
/// </summary>
public sealed record ExportMovementsQuery(
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int? Direction = null,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    string? Search = null,
    int Take = 2000,
    ReportFormat Format = ReportFormat.Excel) : IRequest<Result<ReportFile>>;

internal sealed class ExportMovementsQueryHandler(
    MovementReader reader,
    IMovementReportBuilder reportBuilder
    ) : IRequestHandler<ExportMovementsQuery, Result<ReportFile>>
{
    public async Task<Result<ReportFile>> Handle(
        ExportMovementsQuery request, CancellationToken cancellationToken)
    {
        GetMovementsQuery filter = new(
            request.StartDate,
            request.EndDate,
            request.Direction,
            request.AccountId,
            request.CategoryId,
            request.Search,
            request.Take);

        List<MovementDto> movements = await reader.ReadAsync(filter, cancellationToken);

        MovementReport report = new(
            request.StartDate,
            request.EndDate,
            await DescribeFiltersAsync(request, movements, cancellationToken),
            movements
                .Select(movement => new MovementReportLine(
                    movement.Date,
                    movement.Description,
                    movement.CategoryName,
                    movement.AccountName,
                    movement.AccountKind,
                    movement.CurrencyName,
                    movement.Deposit,
                    movement.Withdrawal,
                    movement.IsTransfer))
                .ToList());

        return reportBuilder.Build(report, request.Format);
    }

    /// <summary>
    /// Uygulanan filtreleri rapor başlığında yazacak hale getirir. Hesap ve kalem
    /// adları, sonuç boş çıktığında satırlardan okunamayacağı için ayrıca aranır.
    /// </summary>
    private async Task<List<MovementReportFilter>> DescribeFiltersAsync(
        ExportMovementsQuery request,
        IReadOnlyList<MovementDto> movements,
        CancellationToken cancellationToken)
    {
        List<MovementReportFilter> filters = [];

        if (request.Direction == 0) filters.Add(new("Yön", "Yalnızca giren"));
        if (request.Direction == 1) filters.Add(new("Yön", "Yalnızca çıkan"));

        if (request.AccountId is { } accountId)
        {
            string name = movements.FirstOrDefault(m => m.AccountId == accountId)?.AccountName
                ?? await reader.AccountNameAsync(accountId, cancellationToken)
                ?? "—";

            filters.Add(new("Hesap", name));
        }

        if (request.CategoryId is { } categoryId)
        {
            string name = movements.FirstOrDefault(m => m.CategoryId == categoryId)?.CategoryName
                ?? await reader.CategoryNameAsync(categoryId, cancellationToken)
                ?? "—";

            filters.Add(new("Kalem", name));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
            filters.Add(new("Arama", request.Search.Trim()));

        return filters;
    }
}

namespace eAccountingServer.Domain.Reporting;

/// <summary>
/// Hareket listesindeki bir satır. Ekstreden farkı, hangi hesaba ait olduğunu
/// da taşıması: bu rapor tek bir hesabı değil, filtreye uyan her hesabı gösterir.
/// </summary>
public sealed record MovementReportLine(
    DateOnly Date,
    string Description,
    string? CategoryName,
    string AccountName,
    string AccountKind,
    string CurrencyName,
    decimal Deposit,
    decimal Withdrawal,
    bool IsTransfer)
{
    public string CurrencySymbol => StatementSymbols.For(CurrencyName);
}

/// <summary>Bir para biriminin dönem toplamı.</summary>
public sealed record MovementReportTotal(
    string CurrencyName,
    decimal Deposit,
    decimal Withdrawal)
{
    public string CurrencySymbol => StatementSymbols.For(CurrencyName);

    public decimal Net => Deposit - Withdrawal;
}

/// <summary>Raporun başlığında gösterilen, uygulanmış bir filtre.</summary>
public sealed record MovementReportFilter(string Label, string Value);

/// <summary>
/// Filtrelenmiş hareket listesinin rapor karşılığı.
///
/// Ekstrede olan yürüyen bakiye sütunu burada yok: liste birden çok hesabı ve
/// birden çok para birimini kapsayabildiği için tek bir yürüyen bakiye anlamsız
/// olurdu. Onun yerine toplamlar para birimi başına ayrı veriliyor.
/// </summary>
public sealed record MovementReport(
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<MovementReportFilter> Filters,
    IReadOnlyList<MovementReportLine> Lines)
{
    public IReadOnlyList<MovementReportTotal> Totals =>
        Lines
            .GroupBy(line => line.CurrencyName)
            .Select(group => new MovementReportTotal(
                group.Key,
                group.Sum(line => line.Deposit),
                group.Sum(line => line.Withdrawal)))
            .OrderBy(total => total.CurrencyName)
            .ToList();

    /// <summary>Başlıkta yazan dönem; uçlardan biri boşsa açık uçlu gösterilir.</summary>
    public string PeriodText() => (StartDate, EndDate) switch
    {
        ({ } start, { } end) => $"{start:dd.MM.yyyy} — {end:dd.MM.yyyy}",
        ({ } start, null) => $"{start:dd.MM.yyyy} tarihinden itibaren",
        (null, { } end) => $"{end:dd.MM.yyyy} tarihine kadar",
        _ => "Tüm zamanlar"
    };

    public string FileNameStem()
    {
        string start = StartDate?.ToString("yyyy-MM-dd") ?? "baslangic";
        string end = EndDate?.ToString("yyyy-MM-dd") ?? "bugun";

        return $"hareketler_{start}_{end}";
    }
}

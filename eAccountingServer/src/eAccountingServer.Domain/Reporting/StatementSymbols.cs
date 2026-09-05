namespace eAccountingServer.Domain.Reporting;

/// <summary>
/// Para birimi adını rapor çıktısındaki sembole çevirir. Arayüzdeki dönüşümün
/// aynısı; ekran ve dosya aynı sembolü göstermeli.
/// </summary>
public static class StatementSymbols
{
    public static string For(string currencyName) => currencyName switch
    {
        "TL" => "₺",
        "USD" => "$",
        "EURO" or "EUR" => "€",
        _ => string.Empty
    };
}

namespace eAccountingServer.Domain.Reporting;

/// <summary>
/// Bir hesap hareketinin rapordaki karşılığı. Kasa ve banka hareketleri farklı
/// tablolarda tutulsa da rapor tarafında aynı biçimde görünür.
/// </summary>
public sealed record StatementLine(
    DateOnly Date,
    string Description,
    decimal Deposit,
    decimal Withdrawal,
    bool IsTransfer);

/// <summary>
/// Seçili tarih aralığındaki hesap ekstresi. Excel ve PDF çıktıları bu tek
/// modelden üretilir, böylece iki dosya da aynı rakamları gösterir.
/// </summary>
public sealed record Statement(
    string AccountKind,
    string AccountName,
    string CurrencyName,
    string CurrencySymbol,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<StatementLine> Lines)
{
    public decimal TotalDeposit => Lines.Sum(line => line.Deposit);

    public decimal TotalWithdrawal => Lines.Sum(line => line.Withdrawal);

    /// <summary>Dönem içindeki net değişim; hesabın toplam bakiyesi değildir.</summary>
    public decimal Net => TotalDeposit - TotalWithdrawal;

    /// <summary>
    /// Dosya adının gövdesi. Türkçe karakterler ve boşluklar indirme sırasında
    /// sorun çıkarabildiği için sadeleştirilir.
    /// </summary>
    public string FileNameStem()
    {
        string name = new(AccountName
            .Select(Simplify)
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            .ToArray());

        if (name.Length == 0) name = AccountKind;

        return $"{name}_{StartDate:yyyy-MM-dd}_{EndDate:yyyy-MM-dd}";
    }

    private static char Simplify(char c) => c switch
    {
        'ç' or 'Ç' => 'c',
        'ğ' or 'Ğ' => 'g',
        'ı' or 'I' => 'i',
        'İ' or 'i' => 'i',
        'ö' or 'Ö' => 'o',
        'ş' or 'Ş' => 's',
        'ü' or 'Ü' => 'u',
        ' ' => '-',
        _ => c
    };
}

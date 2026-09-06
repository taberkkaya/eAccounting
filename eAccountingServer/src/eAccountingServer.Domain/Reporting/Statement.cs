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
/// Ekstrenin sütun başlıkları.
///
/// Kasa ekstresiyle cari ekstresi aynı tabloya oturuyor - tarih, iki tutar, bir
/// koşu bakiyesi - ama muhasebede aynı adları taşımıyorlar. Yapıyı ikiye
/// bölmektense başlıkları değiştirilebilir yapmak, iki çıktının biçim olarak
/// birbirinden kaymasını da engelliyor.
/// </summary>
public sealed record StatementLabels(
    string Title,
    string Debit,
    string Credit,
    string Balance)
{
    public static readonly StatementLabels Account =
        new("Ekstresi", "Giren", "Çıkan", "Dönem Bakiyesi");

    public static readonly StatementLabels Contact =
        new("Ekstresi", "Borç", "Alacak", "Bakiye");
}

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
    /// <summary>
    /// Aralıktan devreden bakiye. Sıfırdan başlayan bir koşu bakiyesi, aralığın
    /// öncesi olan bir hesapta yanlış rakam gösterir.
    /// </summary>
    public decimal OpeningBalance { get; init; }

    public StatementLabels Labels { get; init; } = StatementLabels.Account;

    public decimal TotalDeposit => Lines.Sum(line => line.Deposit);

    public decimal TotalWithdrawal => Lines.Sum(line => line.Withdrawal);

    /// <summary>Dönem içindeki net değişim; hesabın toplam bakiyesi değildir.</summary>
    public decimal Net => TotalDeposit - TotalWithdrawal;

    /// <summary>Devir dahil, dönem sonundaki bakiye.</summary>
    public decimal ClosingBalance => OpeningBalance + Net;

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

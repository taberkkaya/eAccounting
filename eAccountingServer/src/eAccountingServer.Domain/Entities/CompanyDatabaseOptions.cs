namespace eAccountingServer.Domain.Entities;

/// <summary>
/// Yeni firmaların veritabanının kurulacağı sunucu. "Company:DefaultDatabase"
/// bölümünden okunur.
///
/// Yönetilen hosting'de uygulamanın veritabanı oluşturma yetkisi yok; firma
/// eklerken veritabanının elle açılmış olması gerekiyordu. Kendi sunucumuzda
/// böyle bir kısıt olmadığı için firma arayüzden tek adımda kurulabiliyor.
/// </summary>
public sealed class CompanyDatabaseOptions
{
    public const string SectionName = "Company:DefaultDatabase";

    public string Server { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Türetilen veritabanı adlarının başına gelir.</summary>
    public string NamePrefix { get; set; } = "Defter_";

    /// <summary>Sunucu tanımlıysa firma eklerken alanlar boş bırakılabilir.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Server);
}

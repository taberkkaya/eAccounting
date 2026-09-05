using eAccountingServer.Domain.Abstractions;

namespace eAccountingServer.Domain.Entities;

/// <summary>
/// Gelir ya da gider kalemi: "Kira", "Maaş", "Satış" gibi. Hareketler bununla
/// etiketlenince "bu ay kiraya ne verdim" sorusu tek filtreyle cevaplanıyor.
/// </summary>
public sealed class Category : Entity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Hareketin Type alanıyla aynı: 0 gelir, 1 gider.</summary>
    public int Direction { get; set; }
}

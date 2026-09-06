using eAccountingServer.Domain.Abstractions;

namespace eAccountingServer.Domain.Entities;
public sealed class BankDetail : Entity
{
    public Guid BankId { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public decimal WithdrawalAmount { get; set; }
    public Guid? BankDetailId { get; set; }
    public BankDetail? BankDetailOpasite { get; set; }

    /// <summary>Bağlı olduğu gelir/gider kalemi. Zorunlu değil.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Bu hareket bir tahsilat ya da ödeme ise hangi cariye ait. Kasa ekstresinde
    /// "kimden aldık" yazabilmek ve iki tarafı birlikte silebilmek için.
    /// </summary>
    public Guid? ContactId { get; set; }
}

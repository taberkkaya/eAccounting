using eAccountingServer.Domain.Abstractions;

namespace eAccountingServer.Domain.Entities;
public sealed class CashRegisterDetail : Entity
{
    public Guid CashRegisterId { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public decimal WithdrawalAmount { get; set; }
    public Guid? CashRegisterDetailId { get; set; }
    public CashRegisterDetail? CashRegisterDetailOpasite { get; set; }
}

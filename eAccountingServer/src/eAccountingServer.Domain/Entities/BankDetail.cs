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
}

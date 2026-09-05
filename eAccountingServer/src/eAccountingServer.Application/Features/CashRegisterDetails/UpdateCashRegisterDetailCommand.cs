using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.CashRegisterDetails;
public sealed record UpdateCashRegisterDetailCommand(
    Guid Id,
    Guid CashRegisterId,
    DateOnly Date,
    int Type,
    decimal Amount,
    string Description
    ) : IRequest<Result<string>>;

internal sealed class UpdateCashRegisterDetailCommandHandler(
    ICashRegisterRepository cashRegisterRepository,
    ICashRegisterDetailRepository cashRegisterDetailRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<UpdateCashRegisterDetailCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateCashRegisterDetailCommand request, CancellationToken cancellationToken)
    {
        CashRegisterDetail? cashRegisterDetail = await cashRegisterDetailRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (cashRegisterDetail is null)
            return Result<string>.Failure("Kasa hareketi bulunamadı.");

        CashRegister? cashRegister = await cashRegisterRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == cashRegisterDetail.CashRegisterId, cancellationToken);

        if (cashRegister is null)
            return Result<string>.Failure("Kasa bulunamadı.");

        cashRegister.DepositAmount -= cashRegisterDetail.DepositAmount;
        cashRegister.WithdrawalAmount -= cashRegisterDetail.WithdrawalAmount;

        cashRegister.DepositAmount += request.Type == 0 ? request.Amount : 0;
        cashRegister.WithdrawalAmount += request.Type == 1 ? request.Amount : 0;

        cashRegisterDetail.DepositAmount = request.Type == 0 ? request.Amount : 0;
        cashRegisterDetail.WithdrawalAmount = request.Type == 1 ? request.Amount : 0;

        cashRegisterDetail.Description = request.Description;
        cashRegisterDetail.Date = request.Date;

        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("cashRegisters");
        
        return "Kasa hareketi güncellendi.";
    }
}

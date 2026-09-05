using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.CashRegisterDetails;
public sealed record DeleteCashRegisterDetailByIdCommand(
    Guid Id
    ) : IRequest<Result<string>>;


public sealed class DeleteCashRegisterDetailByIdCommandHandler(
    ICashRegisterRepository cashRegisterRepository,
    ICashRegisterDetailRepository cashRegisterDetailRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<DeleteCashRegisterDetailByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteCashRegisterDetailByIdCommand request, CancellationToken cancellationToken)
    {
        CashRegisterDetail? cashRegisterDetail = await cashRegisterDetailRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if(cashRegisterDetail is null)
            return Result<string>.Failure("Kasa hareketi bulunamadı.");

        CashRegister? cashRegister = await cashRegisterRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == cashRegisterDetail.CashRegisterId, cancellationToken);

        if (cashRegister is null)
            return Result<string>.Failure("Kasa bulunamadı.");

        cashRegister.DepositAmount -= cashRegisterDetail.DepositAmount;
        cashRegister.WithdrawalAmount -= cashRegisterDetail.WithdrawalAmount;

        if(cashRegisterDetail.CashRegisterDetailId is not null)
        {
            CashRegisterDetail? oppositeCashRegisterDetail = await cashRegisterDetailRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == cashRegisterDetail.CashRegisterDetailId, cancellationToken);

            if (oppositeCashRegisterDetail is null)
                return Result<string>.Failure("Karşı kasa hareketi bulunamadı.");


            CashRegister? oppositeCashRegister = await cashRegisterRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == oppositeCashRegisterDetail.CashRegisterId, cancellationToken);

            if (oppositeCashRegister is null)
                return Result<string>.Failure("Karşı kasa bulunamadı.");


            // Reverse only what this leg of the transfer contributed; subtracting the
            // running balance from itself would zero the counter account.
            oppositeCashRegister.DepositAmount -= oppositeCashRegisterDetail.DepositAmount;
            oppositeCashRegister.WithdrawalAmount -= oppositeCashRegisterDetail.WithdrawalAmount;

            cashRegisterDetailRepository.Delete(oppositeCashRegisterDetail);
        }

        cashRegisterDetailRepository.Delete(cashRegisterDetail);

        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("cashRegisters");

        return "Kasa hareketi silindi.";
    }
}

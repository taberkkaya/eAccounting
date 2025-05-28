using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.CashRegisterDetails;
public sealed record CreateCashRegisterDetailCommand(
    Guid CashRegisterId,
    DateOnly Date,
    int Type,
    decimal Amount,
    Guid? OppositeCashRegisterId,
    decimal OppositeAmount,
    string Description
    ) : IRequest<Result<string>>;

internal sealed class CreateCashRegisterDetailCommandHandler(
    ICashRegisterRepository cashRegisterRepository,
    ICashRegisterDetailRepository cashRegisterDetailRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<CreateCashRegisterDetailCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateCashRegisterDetailCommand request, CancellationToken cancellationToken)
    {
        CashRegister? cashRegister = await cashRegisterRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.CashRegisterId, cancellationToken);

        cashRegister.DepositAmount += (request.Type == 0 ? request.Amount : 0);
        cashRegister.WithdrawalAmount += (request.Type == 1 ? request.Amount : 0);

        CashRegisterDetail cashRegisterDetail = new()
        {
            Date = request.Date,
            DepositAmount = request.Type == 0 ? request.Amount : 0,
            WithdrawalAmount = request.Type == 1 ? request.Amount : 0,
            Description = request.Description,
            CashRegisterId = request.CashRegisterId
        };

        await cashRegisterDetailRepository.AddAsync(cashRegisterDetail, cancellationToken);

        if (request.OppositeCashRegisterId is not null)
        {
            CashRegister? oppositeCashRegister = await cashRegisterRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == request.OppositeCashRegisterId, cancellationToken);

            if(oppositeCashRegister.CurrencyType.Value != cashRegister.CurrencyType.Value)
            {
                oppositeCashRegister.DepositAmount += (request.Type == 1 ? request.OppositeAmount : 0);
                oppositeCashRegister.WithdrawalAmount += (request.Type == 0 ? request.OppositeAmount : 0);
            }
            else
            {
                oppositeCashRegister.DepositAmount += (request.Type == 1 ? request.Amount : 0);
                oppositeCashRegister.WithdrawalAmount += (request.Type == 0 ? request.Amount : 0);
            }


            CashRegisterDetail oppositeCashRegisterDetail = new()
            {
                Date = request.Date,
                CashRegisterDetailId = cashRegisterDetail.Id,
                Description = request.Description,
                CashRegisterId = (Guid)request.OppositeCashRegisterId
            };

            if (oppositeCashRegister.CurrencyType.Value != cashRegister.CurrencyType.Value)
            {
                oppositeCashRegisterDetail.DepositAmount += (request.Type == 1 ? request.OppositeAmount : 0);
                oppositeCashRegisterDetail.WithdrawalAmount += (request.Type == 0 ? request.OppositeAmount : 0);
            }
            else
            {
                oppositeCashRegisterDetail.DepositAmount += (request.Type == 1 ? request.Amount : 0);
                oppositeCashRegisterDetail.WithdrawalAmount += (request.Type == 0 ? request.Amount : 0);
            }

            cashRegisterDetail.CashRegisterDetailId = oppositeCashRegisterDetail.Id;

            await cashRegisterDetailRepository.AddAsync(oppositeCashRegisterDetail, cancellationToken);
        }

        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("cashRegisters");

        return "Cash register detail created successfully.";
    }
}

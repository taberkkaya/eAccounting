using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.BankDetails;
public sealed record CreateBankDetailCommand(
    Guid BankId,
    DateOnly Date,
    int Type,
    decimal Amount,
    Guid? OppositeBankId,
    decimal OppositeAmount,
    string Description,
    Guid? CategoryId
    ) : IRequest<Result<string>>;

internal sealed class CreateBankDetailHandler(
    IBankRepository bankRepository,
    IBankDetailRepository bankDetailRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<CreateBankDetailCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateBankDetailCommand request, CancellationToken cancellationToken)
    {
        Bank? bank = await bankRepository
      .GetByExpressionWithTrackingAsync(p => p.Id == request.BankId, cancellationToken);

        bank.DepositAmount += (request.Type == 0 ? request.Amount : 0);
        bank.WithdrawalAmount += (request.Type == 1 ? request.Amount : 0);

        BankDetail bankDetail = new()
        {
            Date = request.Date,
            DepositAmount = request.Type == 0 ? request.Amount : 0,
            WithdrawalAmount = request.Type == 1 ? request.Amount : 0,
            Description = request.Description,
            CategoryId = request.CategoryId,
            BankId = request.BankId
        };

        await bankDetailRepository.AddAsync(bankDetail, cancellationToken);

        if (request.OppositeBankId is not null)
        {
            Bank? oppositeBank = await bankRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == request.OppositeBankId, cancellationToken);

            if (oppositeBank.CurrencyType.Value != bank.CurrencyType.Value)
            {
                oppositeBank.DepositAmount += (request.Type == 1 ? request.OppositeAmount : 0);
                oppositeBank.WithdrawalAmount += (request.Type == 0 ? request.OppositeAmount : 0);
            }
            else
            {
                oppositeBank.DepositAmount += (request.Type == 1 ? request.Amount : 0);
                oppositeBank.WithdrawalAmount += (request.Type == 0 ? request.Amount : 0);
            }


            BankDetail oppositeBankDetail = new()
            {
                Date = request.Date,
                BankDetailId = bankDetail.Id,
                Description = request.Description,
                BankId = (Guid)request.OppositeBankId
            };

            if (oppositeBank.CurrencyType.Value != bank.CurrencyType.Value)
            {
                oppositeBankDetail.DepositAmount += (request.Type == 1 ? request.OppositeAmount : 0);
                oppositeBankDetail.WithdrawalAmount += (request.Type == 0 ? request.OppositeAmount : 0);
            }
            else
            {
                oppositeBankDetail.DepositAmount += (request.Type == 1 ? request.Amount : 0);
                oppositeBankDetail.WithdrawalAmount += (request.Type == 0 ? request.Amount : 0);
            }

            bankDetail.BankDetailId = oppositeBankDetail.Id;

            await bankDetailRepository.AddAsync(oppositeBankDetail, cancellationToken);
        }

        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("banks");

        return "Banka hareketi eklendi.";
    }
}

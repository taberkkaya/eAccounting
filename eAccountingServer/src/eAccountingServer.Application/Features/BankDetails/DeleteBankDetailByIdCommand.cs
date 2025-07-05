using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.BankDetails;
public sealed record DeleteBankDetailByIdCommand(
    Guid Id
    ) : IRequest<Result<string>>;

internal sealed class DeleteBankDetailByIdCommandHandler(
    IBankRepository bankRepository,
    IBankDetailRepository bankDetailRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<DeleteBankDetailByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteBankDetailByIdCommand request, CancellationToken cancellationToken)
    {
        BankDetail? bankDetail = await bankDetailRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (bankDetail is null)
            return Result<string>.Failure("Bank detail not found.");

        Bank? bank = await bankRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == bankDetail.BankId, cancellationToken);

        if (bank is null)
            return Result<string>.Failure("Bank not found.");

        bank.DepositAmount -= bankDetail.DepositAmount;
        bank.WithdrawalAmount -= bankDetail.WithdrawalAmount;

        if (bankDetail.BankDetailId is not null)
        {
            BankDetail? oppositeBankDetail = await bankDetailRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == bankDetail.BankDetailId, cancellationToken);

            if (bankDetail is null)
                return Result<string>.Failure("Bank detail not found.");


            Bank oppositeBank = await bankRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == oppositeBankDetail.BankId, cancellationToken);

            if (bank is null)
                return Result<string>.Failure("Bank not found.");


            oppositeBank.DepositAmount -= oppositeBank.DepositAmount;
            oppositeBank.WithdrawalAmount -= oppositeBank.WithdrawalAmount;

            bankDetailRepository.Delete(oppositeBankDetail);
        }

        bankDetailRepository.Delete(bankDetail);

        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("banks");

        return "Bank detail deleted successfully.";
    }
}

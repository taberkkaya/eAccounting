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
            return Result<string>.Failure("Banka hareketi bulunamadı.");

        Bank? bank = await bankRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == bankDetail.BankId, cancellationToken);

        if (bank is null)
            return Result<string>.Failure("Banka bulunamadı.");

        bank.DepositAmount -= bankDetail.DepositAmount;
        bank.WithdrawalAmount -= bankDetail.WithdrawalAmount;

        if (bankDetail.BankDetailId is not null)
        {
            BankDetail? oppositeBankDetail = await bankDetailRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == bankDetail.BankDetailId, cancellationToken);

            if (oppositeBankDetail is null)
                return Result<string>.Failure("Karşı banka hareketi bulunamadı.");


            Bank? oppositeBank = await bankRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == oppositeBankDetail.BankId, cancellationToken);

            if (oppositeBank is null)
                return Result<string>.Failure("Karşı banka bulunamadı.");


            // Reverse only what this leg of the transfer contributed; subtracting the
            // running balance from itself would zero the counter account.
            oppositeBank.DepositAmount -= oppositeBankDetail.DepositAmount;
            oppositeBank.WithdrawalAmount -= oppositeBankDetail.WithdrawalAmount;

            bankDetailRepository.Delete(oppositeBankDetail);
        }

        bankDetailRepository.Delete(bankDetail);

        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("banks");

        return "Banka hareketi silindi.";
    }
}

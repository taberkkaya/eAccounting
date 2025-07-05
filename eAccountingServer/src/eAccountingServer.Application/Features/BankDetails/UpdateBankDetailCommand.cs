using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.BankDetails;
public sealed record UpdateBankDetailCommand(
    Guid Id,
    Guid BankId,
    DateOnly Date,
    int Type,
    decimal Amount,
    string Description
    ) : IRequest<Result<string>>;

internal sealed class UpdateBankDetailCommandHandler(
    IBankRepository bankRepository,
    IBankDetailRepository bankDetailRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<UpdateBankDetailCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateBankDetailCommand request, CancellationToken cancellationToken)
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

        bank.DepositAmount += request.Type == 0 ? request.Amount : 0;
        bank.WithdrawalAmount += request.Type == 1 ? request.Amount : 0;

        bankDetail.DepositAmount = request.Type == 0 ? request.Amount : 0;
        bankDetail.WithdrawalAmount = request.Type == 1 ? request.Amount : 0;

        bankDetail.Description = request.Description;
        bankDetail.Date = request.Date;

        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("banks");

        return "Bank details updated successfully.";
    }
}

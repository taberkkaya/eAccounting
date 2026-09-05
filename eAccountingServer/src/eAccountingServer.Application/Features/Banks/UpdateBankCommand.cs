using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using Mapster;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.Banks;
public sealed record UpdateBankCommand(
    Guid Id,
    string Name,
    string IBAN,
    int CurrencyTypeValue
    ) : IRequest<Result<string>>;

internal sealed class UpdateBankCommandHandler(
    IBankRepository bankRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService) : IRequestHandler<UpdateBankCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateBankCommand request, CancellationToken cancellationToken)
    {
        Bank bank = await bankRepository.GetByExpressionWithTrackingAsync(
            p => p.Id == request.Id,
            cancellationToken: cancellationToken
        );

        if(bank is null)
            return Result<string>.Failure("Banka bulunamadı.");

        if(bank.IBAN != request.IBAN)
        {
            bool isIBANExists = await bankRepository.AnyAsync(p => p.IBAN == request.IBAN);
            if (isIBANExists)
                return Result<string>.Failure("Bu IBAN zaten kayıtlı.");
        }

        request.Adapt(bank);

        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);
        cacheService.Remove("banks");

        return "Banka güncellendi.";
    }
}

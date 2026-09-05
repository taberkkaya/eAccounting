using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using Mapster;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.Banks;
public sealed record CreateBankCommand(
    string Name,
    string IBAN,
    int CurrencyTypeValue
    ) : IRequest<Result<string>>;

internal sealed class CreateBankCommandHandler(
    IBankRepository bankRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<CreateBankCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateBankCommand request, CancellationToken cancellationToken)
    {
        
        bool isIBANExists = await bankRepository.AnyAsync(p => p.IBAN == request.IBAN);
        if(isIBANExists)
            return Result<string>.Failure("Bu IBAN zaten kayıtlı.");

        Bank bank = request.Adapt<Bank>();

        await bankRepository.AddAsync(bank, cancellationToken);
        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("banks");
        
        return "Banka eklendi.";
    }

}
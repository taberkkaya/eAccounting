using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using Mapster;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.CashRegisters;
public sealed record UpdateCacheRegisterCommand(
        Guid Id,
        string Name,
        int CurrencyTypeValue
    ) : IRequest<Result<string>>;

internal sealed class UpdateCacheRegisterCommandHandler(
    ICashRegisterRepository cashRegisterRepository,
    ICacheService cacheService,
    IUnitOfWorkCompany unitOfWorkCompany
    ) : IRequestHandler<UpdateCacheRegisterCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateCacheRegisterCommand request, CancellationToken cancellationToken)
    {
        CashRegister? cashRegister = await cashRegisterRepository.GetByExpressionWithTrackingAsync(
            p => p.Id == request.Id,
            cancellationToken);
        
        if(cashRegister is null)
            return Result<string>.Failure($"Cash register with ID '{request.Id}' does not exist.");

        bool isNameExists = await cashRegisterRepository.AnyAsync(p => p.Name == request.Name && p.Id != request.Id);

        if (isNameExists)
            return Result<string>.Failure($"Cash register with name '{request.Name}' already exists.");

        request.Adapt(cashRegister);

        cashRegisterRepository.Update(cashRegister);
        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("cashRegisters");

        return Result<string>.Succeed("Cash register updated successfully.");
    }
}
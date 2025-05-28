using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using Mapster;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.CashRegisters;
public sealed record CreateCashRegisterCommand(
    string Name,
    int CurrencyTypeValue
    ) : IRequest<Result<string>>;


internal sealed class CreateCashRegisterCommandHandler(
    ICashRegisterRepository cashRegisterRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<CreateCashRegisterCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateCashRegisterCommand request, CancellationToken cancellationToken)
    {
       bool isNameExists = await cashRegisterRepository.AnyAsync(p => p.Name == request.Name);

        if (isNameExists)
            return Result<string>.Failure($"Cash register with name '{request.Name}' already exists.");

        CashRegister cashRegister = request.Adapt<CashRegister>();
        cashRegisterRepository.Add(cashRegister);
        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("cashRegisters");

        return "Cash register created successfully.";
    }
}

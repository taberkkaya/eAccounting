using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.CashRegisters;
public sealed record DeleteCashRegisterByIdCommand(
    Guid Id) : IRequest<Result<string>>;

internal sealed class DeleteCashRegisterByIdCommandHandler(
    ICashRegisterRepository cashRegisterRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<DeleteCashRegisterByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteCashRegisterByIdCommand request, CancellationToken cancellationToken)
    {
        CashRegister? cashRegister = await cashRegisterRepository.GetByExpressionWithTrackingAsync(
            p => p.Id == request.Id,
            cancellationToken
            );

        if(cashRegister is null)
            return Result<string>.Failure($"Cash register with ID '{request.Id}' does not exist.");

        cashRegister.IsDeleted = true;
        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);

        cacheService.Remove("cashRegisters");

        return "Cash register removed successfully.";
    }
}

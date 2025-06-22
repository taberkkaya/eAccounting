using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using MediatR;
using ResultKit;

namespace eAccountingServer.Application.Features.Banks;
public sealed record DeleteBankByIdCommand(
    Guid Id
    ) : IRequest<Result<string>>;


internal sealed class DeleteBankByIdCommandHandler(
    IBankRepository bankRepository,
    IUnitOfWorkCompany unitOfWorkCompany,
    ICacheService cacheService
    ) : IRequestHandler<DeleteBankByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteBankByIdCommand request, CancellationToken cancellationToken)
    {
        Bank bank = await bankRepository.GetByExpressionWithTrackingAsync(
           p => p.Id == request.Id,
           cancellationToken: cancellationToken
       );

        if (bank is null)
            return Result<string>.Failure("Bank not found.");

        bank.IsDeleted = true;

        await unitOfWorkCompany.SaveChangesAsync(cancellationToken);
        
        cacheService.Remove("banks");

        return "Bank deleted successfully.";
    }
}

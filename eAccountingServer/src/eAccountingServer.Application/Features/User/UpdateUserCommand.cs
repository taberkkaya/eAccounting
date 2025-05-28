using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Event;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.Users;
using GenericRepository;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ResultKit;

namespace eAccountingServer.Application.Features.User;
public sealed record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string? Password,
    List<Guid> CompanyIds,
    bool IsAdmin
    ) : IRequest<Result<string>>;

internal sealed class UpdateUserCommandHandler(
    IMediator mediator,
    UserManager<AppUser> userManager,
    ICompanyUserRepository companyUserRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService
    ) : IRequestHandler<UpdateUserCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = await userManager.Users
            .Where(p => p.Id == request.Id)
            .Include(p => p.CompanyUsers)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result<string>.Failure("User not found!");

        bool isEmailExist = await userManager.Users.AnyAsync(p => p.Email == request.Email && p.Id != request.Id);
        if (isEmailExist)
            return Result<string>.Failure("Email already exists!");

        bool isUserNameExist = await userManager.Users.AnyAsync(p => p.UserName == request.UserName && p.Id != request.Id);
        if (isUserNameExist)
            return Result<string>.Failure("UserName already exists!");

        bool isMailChanged = user.Email != request.Email;

        user = request.Adapt(user);


        foreach(var item in user.CompanyUsers!)
            item.IsDeleted = true;

        companyUserRepository.DeleteRange(user.CompanyUsers!);

        List<CompanyUser> companyUsers = request.CompanyIds.Select(companyId => new CompanyUser
        {
            CompanyId = companyId,
            AppUserId = user.Id
        }).ToList();

        await companyUserRepository.AddRangeAsync(companyUsers,cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        cacheService.Remove("users");

        if (isMailChanged)
        {
            user.EmailConfirmed = false;
            await mediator.Publish(new AppUserEvent(user.Id));
        }

        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(s => s.Description).ToList());

        if (request.Password is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            result = await userManager.ResetPasswordAsync(user, token, request.Password);
            if (!result.Succeeded)
                return Result<string>.Failure(result.Errors.Select(s => s.Description).ToList());
        }


        return "User updated successfully!";
    }
}


using eAccountingServer.Domain.Users;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.User;
public sealed record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password
    ) : IRequest<Result<string>>;

internal sealed class UpdateUserCommandHandler(
    UserManager<AppUser> userManager
    ) : IRequestHandler<UpdateUserCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = await userManager.FindByIdAsync(request.Id.ToString());
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

        if (isMailChanged)
            user.EmailConfirmed = false;

        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(s => s.Description).ToList());

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        result = await userManager.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(s => s.Description).ToList());

        return "User updated successfully!";
    }
}


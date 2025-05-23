using eAccountingServer.Domain.Users;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.User;
public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password
    ) : IRequest<Result<string>>;

internal sealed class CreateUserCommandHandler(
    UserManager<AppUser> userManager
    ) : IRequestHandler<CreateUserCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        bool isEmailExist = await userManager.Users.AnyAsync(p => p.Email == request.Email);
        if (isEmailExist)
            return Result<string>.Failure("Email already exists!");

        bool isUserNameExist = await userManager.Users.AnyAsync(p => p.UserName == request.UserName);
        if (isUserNameExist)
            return Result<string>.Failure("UserName already exists!");


        var user = request.Adapt<AppUser>();

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(s => s.Description).ToList());

        return "User created successfully!";
    }
}

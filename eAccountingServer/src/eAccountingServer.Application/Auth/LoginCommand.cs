using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Auth
{
    public sealed record LoginCommand(
        string UserNameOrEmail,
        string Password) : IRequest<Result<LoginCommandResponse>>;

    public sealed record LoginCommandResponse
    {
        public string AccessToken { get; init; } = default!;
    }

    internal sealed class LoginCommandHandler(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJwtProvider jwtProvider
        ) : IRequestHandler<LoginCommand, Result<LoginCommandResponse>>
    {
        public async Task<Result<LoginCommandResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            AppUser? user = await userManager.Users.FirstOrDefaultAsync(
                p => p.Email == request.UserNameOrEmail || p.UserName == request.UserNameOrEmail);

            if (user is null)
                return Result<LoginCommandResponse>.Failure("User not found!");

            SignInResult signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, true);

            if (signInResult.IsLockedOut)
            {
                TimeSpan? timeSpan = user.LockoutEnd - DateTime.UtcNow;
                if (timeSpan is not null)
                    return (500, $"Your password blocked for {Math.Ceiling(timeSpan.Value.TotalMinutes)}.");
                else
                    return (500, "Your password blocked for 5 min.");
            }

            if (signInResult.IsNotAllowed)
            {
                return (500, "Your email address not confirmed!");
            }

            if (!signInResult.Succeeded)
            {
                return (500, "Wrong password!");
            }

            var token = await jwtProvider.CreateTokenAsync(user, request.Password, cancellationToken);

            var response = new LoginCommandResponse() { AccessToken = token };

            return response;
        }
    }
}
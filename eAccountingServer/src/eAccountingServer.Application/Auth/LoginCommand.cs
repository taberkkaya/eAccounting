using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
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
        IJwtProvider jwtProvider,
        ICompanyUserRepository companyUserRepository
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

            List<CompanyUser> companyUsers = await companyUserRepository
                .Where(p => p.AppUserId == user.Id)
                .Include(p => p.Company!)
                .ToListAsync(cancellationToken);

            Guid? companyId = null;

            List<Company> companies = new();

            if (companyUsers.Count > 0)
            {
                companyId = companyUsers.First().CompanyId;
                companies = companyUsers.Select(s => new Company
                {
                    Id = s.CompanyId,
                    Name = s.Company!.Name,
                    TaxDepartment = s.Company!.TaxDepartment,
                    TaxNumber = s.Company!.TaxNumber,
                    CreatedAt = s.CreatedAt,
                    CreatedBy = s.CreatedBy,
                }).ToList();
            }

            var token = await jwtProvider.CreateTokenAsync(user, companyId, companies, cancellationToken);

            var response = new LoginCommandResponse() { AccessToken = token };

            return response;
        }
    }
}
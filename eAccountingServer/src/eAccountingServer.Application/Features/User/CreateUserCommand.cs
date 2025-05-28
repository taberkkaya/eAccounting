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
using Microsoft.VisualBasic;
using ResultKit;

namespace eAccountingServer.Application.Features.User;
public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password,
    List<Guid> CompanyIds,
    bool IsAdmin
    ) : IRequest<Result<string>>;

internal sealed class CreateUserCommandHandler(
    IMediator mediator,
    UserManager<AppUser> userManager,
    ICompanyUserRepository companyUserRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService
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

        List<CompanyUser> companyUsers = request.CompanyIds.Select(s => new CompanyUser { 
            AppUserId = user.Id,
            CompanyId = s 
        }).ToList();

        await companyUserRepository.AddRangeAsync(companyUsers, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        cacheService.Remove("users");

        await mediator.Publish(new AppUserEvent(user.Id));


        return "User created successfully!";
    }
}

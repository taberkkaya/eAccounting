using System.ComponentModel.Design;
using System.Security.Claims;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Auth;
public sealed record ChangeCompanyCommand(Guid CompanyId) : IRequest<Result<LoginCommandResponse>>;

internal sealed class ChangeCompanyCommandHandler(
    ICompanyUserRepository companyUserRepository,
    UserManager<AppUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IJwtProvider jwtProvider,
    ICacheService cacheService
    ) : IRequestHandler<ChangeCompanyCommand, Result<LoginCommandResponse>>
{
    public async Task<Result<LoginCommandResponse>> Handle(ChangeCompanyCommand request, CancellationToken cancellationToken)
    {
        if (httpContextAccessor.HttpContext is null)
            return Result<LoginCommandResponse>.Failure("Bu işlemi yapmaya yetkiniz yok.");

        string? userIdString = httpContextAccessor.HttpContext.User.FindFirstValue("Id");

        if(string.IsNullOrEmpty(userIdString))
            return Result<LoginCommandResponse>.Failure("Bu işlemi yapmaya yetkiniz yok.");


        AppUser? user = await userManager.FindByIdAsync(userIdString);

        if(user == null)
            return Result<LoginCommandResponse>.Failure("Kullanıcı bulunamadı.");

        List<CompanyUser> companyUsers = await companyUserRepository
            .Where(p => p.AppUserId == user.Id)
            .Include(p => p.Company)
            .ToListAsync(cancellationToken);
        List<Company> companies = new();

        if (companyUsers.Count > 0)
        {
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

        var token = await jwtProvider.CreateTokenAsync(user, request.CompanyId, companies, cancellationToken);

        var response = new LoginCommandResponse() { AccessToken = token };

        cacheService.Remove("cashRegisters");
        cacheService.Remove("banks");

        return response;
    }
}

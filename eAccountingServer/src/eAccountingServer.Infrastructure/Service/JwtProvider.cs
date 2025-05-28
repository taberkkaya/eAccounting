using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Users;
using eAccountingServer.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace eAccountingServer.Infrastructure.Service
{
    internal sealed class JwtProvider(IOptions<JwtOptions> jwtOptions) : IJwtProvider
    {
        public Task<string> CreateTokenAsync(AppUser user, Guid? companyId, List<Company> companies, CancellationToken cancellationToken = default)
        {
            List<Claim> claims = new()
            {
                new Claim("Id",user.Id.ToString()),
                new Claim("Name", user.FirstName +" "+user.LastName),
                new Claim("UserName", user.UserName ?? string.Empty),
                new Claim("Email", user.Email ?? string.Empty),
                new Claim("CompanyId", companyId.ToString() ?? string.Empty),
                new Claim("Companies", JsonSerializer.Serialize(companies)),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            };


            var expires = DateTime.Now.AddDays(1);

            SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey));
            SigningCredentials signingCredentials = new(securityKey, SecurityAlgorithms.HmacSha512);

            JwtSecurityToken JwtSecurityToken = new(
                issuer: jwtOptions.Value.Issuer,
                audience: jwtOptions.Value.Audience,
                claims: claims,
                notBefore: DateTime.Now,
                expires: expires,
                signingCredentials: signingCredentials
                );

            JwtSecurityTokenHandler handler = new();

            string token = handler.WriteToken(JwtSecurityToken);

            return Task.FromResult(token);
        }
    }
}

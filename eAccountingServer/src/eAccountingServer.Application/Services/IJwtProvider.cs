using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Users;

namespace eAccountingServer.Application.Services
{
    public interface IJwtProvider
    {
        public Task<string> CreateTokenAsync(AppUser user,Guid? companyId, List<Company> companies, CancellationToken cancellationToken = default);
    }
}

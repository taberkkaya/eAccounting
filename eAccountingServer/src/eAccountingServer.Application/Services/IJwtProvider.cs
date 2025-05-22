using eAccountingServer.Domain.Users;

namespace eAccountingServer.Application.Services
{
    public interface IJwtProvider
    {
        public Task<string> CreateTokenAsync(AppUser user, string password, CancellationToken cancellationToken = default);
    }
}

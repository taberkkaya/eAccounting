using eAccountingServer.Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace eAccountingServer.Domain.Abstractions
{
    public class Entity
    {
        public Guid Id { get; set; }

        public Entity()
        {
            Id = Guid.CreateVersion7();
        }

        public DateTimeOffset CreatedAt { get; set; } = default!;
        public Guid CreatedBy { get; set; } = default!;
        public string CreateUserName => GetCreateUserName();

        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public string? UpdateUserName => GetUpdateUserName();

        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;

        private string GetCreateUserName()
        {
            HttpContextAccessor httpContextAccessor = new();
            var userManager = httpContextAccessor
                .HttpContext
                .RequestServices
                .GetRequiredService<UserManager<AppUser>>();

            AppUser appUser = userManager.Users.First(p => p.Id == CreatedBy);

            return appUser.FirstName + " " + appUser.LastName + " (" + appUser.Email + ")";
        }

        private string? GetUpdateUserName()
        {
            if (UpdatedBy is null) return null;

            HttpContextAccessor httpContextAccessor = new();
            var userManager = httpContextAccessor
                .HttpContext
                .RequestServices
                .GetRequiredService<UserManager<AppUser>>();

            AppUser appUser = userManager.Users.First(p => p.Id == UpdatedBy);

            return appUser.FirstName + " " + appUser.LastName + " (" + appUser.Email + ")";
        }
    }
}

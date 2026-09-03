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
        public string CreateUserName => ResolveUserName(CreatedBy) ?? string.Empty;

        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public string? UpdateUserName => ResolveUserName(UpdatedBy);

        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Resolved lazily during serialization. Rows can outlive the user that wrote
        /// them, and entities are also materialized outside a request (seeding, hosted
        /// services) where there is no HttpContext to resolve a UserManager from, so a
        /// missing name is never treated as an error.
        /// </summary>
        private static string? ResolveUserName(Guid? userId)
        {
            if (userId is null || userId == Guid.Empty) return null;

            HttpContext? httpContext = new HttpContextAccessor().HttpContext;
            if (httpContext is null) return null;

            UserManager<AppUser>? userManager = httpContext
                .RequestServices
                .GetService<UserManager<AppUser>>();

            AppUser? appUser = userManager?.Users.FirstOrDefault(p => p.Id == userId);
            if (appUser is null) return null;

            return $"{appUser.FirstName} {appUser.LastName} ({appUser.Email})";
        }
    }
}

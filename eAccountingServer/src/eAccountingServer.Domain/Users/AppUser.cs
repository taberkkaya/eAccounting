using eAccountingServer.Domain.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace eAccountingServer.Domain.Users
{
    public sealed class AppUser : IdentityUser<Guid>
    {
        public AppUser()
        {
            Id = Guid.CreateVersion7();
        }

        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string FullName => $"{FirstName} {LastName}";

        public DateTimeOffset CreatedAt { get; set; } = default!;
        public Guid CreatedBy { get; set; } = default!;

        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}

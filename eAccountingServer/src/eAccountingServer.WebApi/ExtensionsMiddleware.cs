using eAccountingServer.Domain.Users;
using eAccountingServer.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace eAccountingServer.WebApi
{
    public static class ExtensionsMiddleware
    {
        /// <summary>
        /// Brings the main database up to date at boot. Off by default because applying
        /// migrations automatically is a deployment decision, not a library one.
        /// </summary>
        public static void MigrateDatabase(WebApplication app)
        {
            if (!app.Configuration.GetValue("Database:MigrateOnStartup", false)) return;

            InfrastructureRegistrar.MigrateApplicationDatabase(app.Services);
        }

        public static void CreateFirstUser(WebApplication app)
        {
            using (var scoped = app.Services.CreateScope())
            {
                var userManager = scoped.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var configuration = scoped.ServiceProvider.GetRequiredService<IConfiguration>();

                string userName = configuration["Seed:AdminUserName"] ?? "admin";

                if (!userManager.Users.Any(p => p.UserName == userName))
                {
                    AppUser user = new()
                    {
                        UserName = userName,
                        Email = configuration["Seed:AdminEmail"] ?? "admin@admin.com",
                        FirstName = "admin",
                        LastName = "admin",
                        EmailConfirmed = true,
                        // Without this the seeded account cannot reach the admin screens
                        // it exists to administer.
                        IsAdmin = true,
                        CreatedAt = DateTimeOffset.Now
                    };

                    user.CreatedBy = user.Id;

                    // Override in production via Seed__AdminPassword.
                    string password = configuration["Seed:AdminPassword"] ?? "1";

                    userManager.CreateAsync(user, password).Wait();
                }
            }
        }
    }
}

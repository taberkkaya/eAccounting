using eAccountingServer.Domain.Users;
using eAccountingServer.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        /// <summary>
        /// Bootstraps the first administrator so a fresh deployment can be signed into.
        /// It only ever runs on an empty system: once a real administrator exists, or
        /// the username is taken, seeding is skipped.
        /// </summary>
        public static void CreateFirstUser(WebApplication app)
        {
            using var scoped = app.Services.CreateScope();
            var userManager = scoped.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var configuration = scoped.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scoped.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(ExtensionsMiddleware));

            string userName = configuration["Seed:AdminUserName"] ?? "admin";

            // The username carries a unique index and users are soft deleted, so the
            // check has to see rows the query filter hides. Without this, deleting the
            // seeded account made the next startup try to recreate it and crash on the
            // duplicate key.
            bool userNameTaken = userManager.Users
                .IgnoreQueryFilters()
                .Any(p => p.UserName == userName);

            // Somebody already runs this system; it does not need a seeded account.
            bool administratorExists = userManager.Users.Any(p => p.IsAdmin);
            if (administratorExists) return;

            if (userNameTaken)
            {
                // No administrator is left, yet the name is still held by a soft deleted
                // row, so a fresh one cannot be inserted. Reaching this state takes a
                // direct edit to the database, but without a way back the system would
                // be permanently unadministrable.
                ReviveSeededAdministrator(userManager, userName, logger);
                return;
            }

            AppUser user = new()
            {
                UserName = userName,
                Email = configuration["Seed:AdminEmail"] ?? "admin@admin.com",
                FirstName = "admin",
                LastName = "admin",
                EmailConfirmed = true,
                // Without this the seeded account cannot reach the admin screens it
                // exists to administer.
                IsAdmin = true,
                CreatedAt = DateTimeOffset.Now
            };

            user.CreatedBy = user.Id;

            // Override in production via Seed__AdminPassword.
            string password = configuration["Seed:AdminPassword"] ?? "1";

            IdentityResult result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();

            if (result.Succeeded)
            {
                logger.LogInformation("Seeded the first administrator ({UserName}).", userName);
                return;
            }

            // Never fatal: the application must still start so the problem can be seen
            // and fixed through the running system.
            logger.LogError(
                "The first administrator could not be seeded: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        private static void ReviveSeededAdministrator(
            UserManager<AppUser> userManager, string userName, ILogger logger)
        {
            AppUser? user = userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefault(p => p.UserName == userName);

            if (user is null) return;

            user.IsDeleted = false;
            user.IsAdmin = true;

            IdentityResult result = userManager.UpdateAsync(user).GetAwaiter().GetResult();

            if (result.Succeeded)
            {
                logger.LogWarning(
                    "No administrator was left, so {UserName} was restored to keep the system reachable.",
                    userName);
                return;
            }

            logger.LogError(
                "No administrator is left and {UserName} could not be restored: {Errors}",
                userName,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}

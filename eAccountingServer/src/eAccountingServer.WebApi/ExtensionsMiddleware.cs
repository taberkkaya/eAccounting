using eAccountingServer.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace eAccountingServer.WebApi
{
    public static class ExtensionsMiddleware
    {
        public static void CreateFirstUser(WebApplication app)
        {
            using (var scoped = app.Services.CreateScope())
            {
                var userManager = scoped.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

                if (!userManager.Users.Any(p => p.UserName == "admin"))
                {
                    AppUser user = new()
                    {
                        UserName = "admin",
                        Email = "admin@admin.com",
                        FirstName = "admin",
                        LastName = "admin",
                        EmailConfirmed = true,
                        CreatedAt = DateTimeOffset.Now
                    };

                    user.CreatedBy = user.Id;

                    userManager.CreateAsync(user, "1").Wait();
                }
            }
        }
    }
}

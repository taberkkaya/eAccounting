using eAccountingServer.Application.Features.DemoVisitors;
using eAccountingServer.Application.Services;
using eAccountingServer.Domain.Demo;
using eAccountingServer.Domain.Repositories;
using eAccountingServer.Domain.Users;
using eAccountingServer.Infrastructure.Context;
using eAccountingServer.Infrastructure.Demo;
using eAccountingServer.Infrastructure.Options;
using eAccountingServer.Infrastructure.Service;
using GenericRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using StackExchange.Redis;

namespace eAccountingServer.Infrastructure
{
    public static class InfrastructureRegistrar
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            // Konum araması dışarıya HTTP çağrısı yapıyor.
            services.AddHttpClient();

            services.AddScoped<CompanyDbContext>();

            services.AddDbContext<ApplicationDbContext>(opt =>
            {
                string connectionString = configuration
                .GetConnectionString("SqlServer")!;

                opt.UseSqlServer(connectionString);
            });


            services.AddIdentity<AppUser, IdentityRole<Guid>>(opt =>
            {
                opt.Password.RequiredLength = 1;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireDigit = false;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireUppercase = false;
                opt.Lockout.MaxFailedAccessAttempts = 5;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                opt.SignIn.RequireConfirmedEmail = configuration.GetValue("Identity:RequireConfirmedEmail", true);
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.ConfigureOptions<JwtOptionsSetup>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();

            services.AddAuthorization(options =>
            {
                // User and company management is administration, not ordinary use, so it
                // is gated on the claim the token already carries rather than on merely
                // being signed in.
                options.AddPolicy(AuthorizationPolicies.Admin, policy =>
                    policy.RequireClaim("IsAdmin", bool.TrueString));
            });

            services.AddScoped<IUnitOfWork>(srv => srv.GetRequiredService<ApplicationDbContext>());

            services.AddScoped<IUnitOfWorkCompany>(srv => srv.GetRequiredService<CompanyDbContext>());

            services.AddScoped<CacheKeyScope>();
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();
            //services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost"));
            //services.AddScoped<ICacheService,RedisCacheService>();

            services.AddDemo(configuration);

            services.Scan(opt => opt
                .FromAssemblies(typeof(InfrastructureRegistrar).Assembly)
                // The demo services own their lifetimes (the slot pool is a singleton and
                // the janitor is a hosted service), so they are wired up by hand above.
                .AddClasses(classes => classes.NotInNamespaceOf<DemoSessionService>(), publicOnly: false)
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            );

            return services;
        }

        /// <summary>
        /// Applies the main database's migrations. Lives here because the context is
        /// internal to this assembly.
        /// </summary>
        public static void MigrateApplicationDatabase(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            context.Database.Migrate();
        }

        private static IServiceCollection AddDemo(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DemoOptions>(configuration.GetSection(DemoOptions.SectionName));

            services.AddScoped<IDemoContext, DemoContext>();

            // Demo ad alanı Scrutor taramasının dışında bırakıldığı için elle.
            services.AddScoped<IDemoVerificationService, DemoVerificationService>();
            services.AddScoped<IDemoVisitorReader, DemoVisitorReader>();

            // One pool of sandbox tenants for the whole process.
            services.AddSingleton<DemoSessionService>();
            services.AddSingleton<IDemoSessionService>(srv => srv.GetRequiredService<DemoSessionService>());
            services.AddHostedService<DemoHostedService>();

            return services;
        }
    }
}

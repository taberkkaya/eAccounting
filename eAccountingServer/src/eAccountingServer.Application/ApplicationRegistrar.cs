using eAccountingServer.Application.Behaviors;
using eAccountingServer.Application.Mapping;
using eAccountingServer.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace eAccountingServer.Application
{
    public static class ApplicationRegistrar
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            MapsterConfig.RegisterMappings();

            services.AddFluentEmail("info@eaccounting.com").AddSmtpSender("localhost", 2525);

            services.AddMediatR(conf =>
            {
                conf.RegisterServicesFromAssemblies(
                    typeof(ApplicationRegistrar).Assembly,
                    typeof(AppUser).Assembly);
                conf.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(ApplicationRegistrar).Assembly);

            return services;
        }
    }
}

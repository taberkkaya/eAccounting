using eAccountingServer.Application.Behaviors;
using eAccountingServer.Application.Mail;
using eAccountingServer.Application.Mapping;
using eAccountingServer.Domain.Users;
using FluentEmail.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eAccountingServer.Application
{
    public static class ApplicationRegistrar
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            MapsterConfig.RegisterMappings();

            services.AddEmail(configuration);

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

        private static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
        {
            string fromAddress = configuration["Mail:From"] ?? "info@eaccounting.com";
            string? host = configuration["Mail:SmtpHost"];

            var builder = services.AddFluentEmail(fromAddress);

            if (string.IsNullOrWhiteSpace(host))
            {
                services.AddSingleton<ISender, NullEmailSender>();
                return services;
            }

            builder.AddSmtpSender(host, configuration.GetValue("Mail:SmtpPort", 25));

            return services;
        }
    }
}

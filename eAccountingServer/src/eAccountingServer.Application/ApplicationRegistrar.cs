using System.Net;
using System.Net.Mail;
using eAccountingServer.Application.Behaviors;
using eAccountingServer.Application.Mail;
using eAccountingServer.Application.Mapping;
using eAccountingServer.Domain.Mail;
using eAccountingServer.Domain.Entities;
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

            // Yeni firmaların veritabanının kurulacağı sunucu.
            services.Configure<CompanyDatabaseOptions>(
                configuration.GetSection(CompanyDatabaseOptions.SectionName));

            services.AddMediatR(conf =>
            {
                conf.RegisterServicesFromAssemblies(
                    typeof(ApplicationRegistrar).Assembly,
                    typeof(AppUser).Assembly);
                conf.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(ApplicationRegistrar).Assembly);

            // İki hareket tablosunu birleştiren okuyucu; iki sorgu da onu kullanıyor.
            services.AddScoped<Features.Movements.MovementReader>();

            // Cariye ve kasaya birlikte yazan defter; fatura ve tahsilat ondan geçiyor.
            services.AddScoped<Features.Accounting.AccountingLedger>();

            return services;
        }

        private static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
        {
            IConfigurationSection section = configuration.GetSection(MailOptions.SectionName);
            services.Configure<MailOptions>(section);

            MailOptions options = section.Get<MailOptions>() ?? new MailOptions();

            var builder = services.AddFluentEmail(
                options.From,
                string.IsNullOrWhiteSpace(options.FromName) ? null : options.FromName);

            if (string.IsNullOrWhiteSpace(options.SmtpHost))
            {
                services.AddSingleton<ISender, NullEmailSender>();
                return services;
            }

            // Gerçek mail sunucuları neredeyse her zaman kimlik doğrulama ve TLS ister;
            // yalnızca host ve port veren kurulum onlara bağlanamaz.
            builder.AddSmtpSender(() =>
            {
                var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
                {
                    EnableSsl = options.UseSsl,
                };

                if (!string.IsNullOrWhiteSpace(options.Username))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(options.Username, options.Password);
                }

                return client;
            });

            return services;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.EmailSenderSmtp
{
    public static class EmailSenderSmtpModuleExtensions
    {
        public static IServiceCollection AddEmailSenderSmtpModule(this IServiceCollection services)
        {
            services.AddScoped<IEmailSender, EmailSender>();
            return services;
        }
    }
}

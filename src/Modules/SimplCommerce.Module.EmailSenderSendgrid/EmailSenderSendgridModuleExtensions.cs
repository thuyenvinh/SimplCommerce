using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.EmailSenderSendgrid
{
    public static class EmailSenderSendgridModuleExtensions
    {
        public static IServiceCollection AddEmailSenderSendgridModule(this IServiceCollection services)
        {
            services.AddScoped<IEmailSender, EmailSender>();
            return services;
        }
    }
}

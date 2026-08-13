using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.Payments
{
    public static class PaymentsModuleExtensions
    {
        public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
        {
            return services;
        }
    }
}

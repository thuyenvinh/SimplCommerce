using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.PaymentMomo
{
    public static class PaymentMomoModuleExtensions
    {
        public static IServiceCollection AddPaymentMomoModule(this IServiceCollection services)
        {
            return services;
        }
    }
}

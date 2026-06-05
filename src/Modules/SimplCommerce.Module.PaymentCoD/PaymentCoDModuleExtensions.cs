using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.PaymentCoD
{
    public static class PaymentCoDModuleExtensions
    {
        public static IServiceCollection AddPaymentCoDModule(this IServiceCollection services)
        {
            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentCoD");
            return services;
        }
    }
}

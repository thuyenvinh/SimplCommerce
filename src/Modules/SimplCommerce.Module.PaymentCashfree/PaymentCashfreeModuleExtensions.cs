using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.PaymentCashfree
{
    public static class PaymentCashfreeModuleExtensions
    {
        public static IServiceCollection AddPaymentCashfreeModule(this IServiceCollection services)
        {
            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentCashfree");
            return services;
        }
    }
}

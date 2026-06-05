using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.PaymentStripe
{
    public static class PaymentStripeModuleExtensions
    {
        public static IServiceCollection AddPaymentStripeModule(this IServiceCollection services)
        {
            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentStripe");
            return services;
        }
    }
}

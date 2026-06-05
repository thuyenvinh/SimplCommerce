using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.PaymentPaypalExpress
{
    public static class PaymentPaypalExpressModuleExtensions
    {
        public static IServiceCollection AddPaymentPaypalExpressModule(this IServiceCollection services)
        {
            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentPaypalExpress");
            return services;
        }
    }
}

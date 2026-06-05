using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.PaymentBraintree.Services;

namespace SimplCommerce.Module.PaymentBraintree
{
    public static class PaymentBraintreeModuleExtensions
    {
        public static IServiceCollection AddPaymentBraintreeModule(this IServiceCollection services)
        {
            services.AddTransient<IBraintreeConfiguration, BraintreeConfiguration>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentBraintree");
            return services;
        }
    }
}

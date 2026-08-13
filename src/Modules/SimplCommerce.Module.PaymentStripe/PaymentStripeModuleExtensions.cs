using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.PaymentStripe.Services;
using SimplCommerce.Module.Payments.Services;

namespace SimplCommerce.Module.PaymentStripe
{
    public static class PaymentStripeModuleExtensions
    {
        public static IServiceCollection AddPaymentStripeModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Wave 12: bind Stripe:SecretKey + register Connect transfer service +
            // its IPayoutGateway adapter so the Vendors payout pipeline can
            // dispatch via Stripe without taking a direct Stripe.net dependency.
            services.Configure<StripeConnectOptions>(configuration.GetSection(StripeConnectOptions.SectionName));
            services.AddSingleton<IStripeConnectService, StripeConnectService>();
            services.AddSingleton<IPayoutGateway, StripeConnectPayoutGateway>();
            return services;
        }
    }
}

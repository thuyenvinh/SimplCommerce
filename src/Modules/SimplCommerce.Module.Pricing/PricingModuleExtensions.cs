using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Pricing.Services;

namespace SimplCommerce.Module.Pricing
{
    public static class PricingModuleExtensions
    {
        public static IServiceCollection AddPricingModule(this IServiceCollection services)
        {
            services.AddTransient<ICouponService, CouponService>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.pricing");
            return services;
        }
    }
}

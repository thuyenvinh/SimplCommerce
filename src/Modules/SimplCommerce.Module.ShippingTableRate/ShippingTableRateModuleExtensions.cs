using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.ShippingPrices.Services;
using SimplCommerce.Module.ShippingTableRate.Services;

namespace SimplCommerce.Module.ShippingTableRate
{
    public static class ShippingTableRateModuleExtensions
    {
        public static IServiceCollection AddShippingTableRateModule(this IServiceCollection services)
        {
            services.AddTransient<IShippingPriceServiceProvider, TableRateShippingServiceProvider>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.shipping-tablerate");
            return services;
        }
    }
}

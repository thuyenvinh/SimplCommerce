using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.ShippingPrices.Services;

namespace SimplCommerce.Module.ShippingPrices
{
    public static class ShippingPricesModuleExtensions
    {
        public static IServiceCollection AddShippingPricesModule(this IServiceCollection services)
        {
            services.AddTransient<IShippingPriceService, ShippingPriceService>();
            return services;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.ShippingFree.Services;
using SimplCommerce.Module.ShippingPrices.Services;

namespace SimplCommerce.Module.ShippingFree
{
    public static class ShippingFreeModuleExtensions
    {
        public static IServiceCollection AddShippingFreeModule(this IServiceCollection services)
        {
            services.AddTransient<IShippingPriceServiceProvider, FreeShippingServiceProvider>();
            return services;
        }
    }
}

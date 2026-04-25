using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.Shipping
{
    public static class ShippingModuleExtensions
    {
        public static IServiceCollection AddShippingModule(this IServiceCollection services)
        {
            GlobalConfiguration.RegisterAngularModule("simplAdmin.shippings");
            return services;
        }
    }
}

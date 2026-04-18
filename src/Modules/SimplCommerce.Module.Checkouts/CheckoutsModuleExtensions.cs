using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Checkouts.Services;

namespace SimplCommerce.Module.Checkouts
{
    public static class CheckoutsModuleExtensions
    {
        public static IServiceCollection AddCheckoutsModule(this IServiceCollection services)
        {
            services.AddTransient<ICheckoutService, CheckoutService>();
            return services;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.WishList.Services;

namespace SimplCommerce.Module.WishList
{
    public static class WishListModuleExtensions
    {
        public static IServiceCollection AddWishListModule(this IServiceCollection services)
        {
            services.AddTransient<IWishListService, WishListService>();
            return services;
        }
    }
}

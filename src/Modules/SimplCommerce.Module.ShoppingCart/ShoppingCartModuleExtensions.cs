using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Core.Events;
using SimplCommerce.Module.ShoppingCart.Events;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.ShoppingCart
{
    public static class ShoppingCartModuleExtensions
    {
        public static IServiceCollection AddShoppingCartModule(this IServiceCollection services)
        {
            services.AddTransient<ICartService, CartService>();
            services.AddTransient<INotificationHandler<UserSignedIn>, UserSignedInHandler>();
            return services;
        }
    }
}

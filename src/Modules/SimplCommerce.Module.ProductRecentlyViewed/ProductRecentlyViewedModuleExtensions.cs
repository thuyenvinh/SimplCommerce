using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Core.Events;
using SimplCommerce.Module.ProductRecentlyViewed.Data;
using SimplCommerce.Module.ProductRecentlyViewed.Events;

namespace SimplCommerce.Module.ProductRecentlyViewed
{
    public static class ProductRecentlyViewedModuleExtensions
    {
        public static IServiceCollection AddProductRecentlyViewedModule(this IServiceCollection services)
        {
            services.AddTransient<IRecentlyViewedProductRepository, RecentlyViewedProductRepository>();
            services.AddTransient<INotificationHandler<EntityViewed>, EntityViewedHandler>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.recentlyViewed");
            return services;
        }
    }
}

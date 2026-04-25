using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Inventory.Event;
using SimplCommerce.Module.Inventory.Services;

namespace SimplCommerce.Module.Inventory
{
    public static class InventoryModuleExtensions
    {
        public static IServiceCollection AddInventoryModule(this IServiceCollection services)
        {
            services.AddTransient<IStockService, StockService>();
            services.AddTransient<IStockSubscriptionService, StockSubscriptionService>();
            services.AddTransient<INotificationHandler<ProductBackInStock>, ProductBackInStockSendEmailHandler>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.inventory");
            return services;
        }
    }
}

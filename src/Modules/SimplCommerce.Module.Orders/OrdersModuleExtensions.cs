using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Orders.Events;
using SimplCommerce.Module.Orders.Services;

namespace SimplCommerce.Module.Orders
{
    public static class OrdersModuleExtensions
    {
        public static IServiceCollection AddOrdersModule(this IServiceCollection services)
        {
            services.AddTransient<IOrderService, OrderService>();
            services.AddTransient<IOrderEmailService, OrderEmailService>();
            services.AddHostedService<OrderCancellationBackgroundService>();
            services.AddTransient<INotificationHandler<OrderChanged>, OrderChangedCreateOrderHistoryHandler>();
            services.AddTransient<INotificationHandler<OrderCreated>, OrderCreatedCreateOrderHistoryHandler>();
            services.AddTransient<INotificationHandler<OrderCreated>, OrderCreatedClearCartHandler>();
            // services.AddTransient<INotificationHandler<AfterOrderCreated>, AfterOrderCreatedSendEmailHanlder>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.orders");
            return services;
        }
    }
}

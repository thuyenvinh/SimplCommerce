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
            // W3-G09: re-enabled after OrderEmailService stopped needing
            // IRazorViewRenderer (which was never wired in the minimal-API host).
            services.AddTransient<INotificationHandler<AfterOrderCreated>, AfterOrderCreatedSendEmailHanlder>();
            return services;
        }
    }
}

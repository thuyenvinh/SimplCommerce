using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Orders.Events;
using SimplCommerce.Module.Shipments.Events;
using SimplCommerce.Module.Shipments.Services;

namespace SimplCommerce.Module.Shipments
{
    public static class ShipmentsModuleExtensions
    {
        public static IServiceCollection AddShipmentsModule(this IServiceCollection services)
        {
            services.AddTransient<INotificationHandler<OrderDetailGot>, OrderDetailGotHandler>();
            services.AddTransient<IShipmentService, ShipmentService>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.shipment");
            return services;
        }
    }
}

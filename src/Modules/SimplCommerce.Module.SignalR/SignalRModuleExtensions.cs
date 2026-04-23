using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.SignalR.Hubs;
using SimplCommerce.Module.SignalR.RealTime;

namespace SimplCommerce.Module.SignalR
{
    public static class SignalRModuleExtensions
    {
        public static IServiceCollection AddSignalRModule(this IServiceCollection services)
        {
            services.AddSignalR();
            services.AddSingleton<IOnlineClientManager, OnlineClientManager>();
            return services;
        }

        /// <summary>
        /// Maps the SignalR hubs. Call this from the composition root AFTER UseRouting()
        /// so the hub endpoints participate in endpoint routing.
        /// </summary>
        public static IEndpointRouteBuilder MapSignalRModule(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<CommonHub>("/signalr");
            return endpoints;
        }
    }
}

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure.Modules;
using SimplCommerce.Module.SignalR.Hubs;

namespace SimplCommerce.Module.SignalR
{
    [Obsolete("Call services.AddSignalRModule() and endpoints.MapSignalRModule() in your composition root.")]
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services) =>
            services.AddSignalRModule();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Legacy shim: IApplicationBuilder.UseSignalR was removed in ASP.NET Core 3.0.
            // Wrap in UseEndpoints so legacy callers via ConfigureModules() still get the
            // hub mapped. New composition roots call MapSignalRModule() directly instead.
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<CommonHub>("/signalr");
            });
        }
    }
}

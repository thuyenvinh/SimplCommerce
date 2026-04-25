using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure.Modules;
using SimplCommerce.Module.Core;
using SimplCommerce.Module.Notifications.Services;

namespace SimplCommerce.Module.Notifications
{
    [Obsolete("Call services.AddNotificationsModule() in your composition root.")]
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services) =>
            services.AddNotificationsModule();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.AddSettingDefinitionItems(SettingDefinitions.DefaultItems());
            app.AddNotificationDefinitionItems(NotificationDefinitions.DefaultItems());
        }
    }
}

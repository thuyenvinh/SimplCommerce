using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.Core
{
    /// <summary>
    /// Forward path for module registration. The legacy reflection-driven
    /// <see cref="ModuleInitializer"/> still works, but new composition roots
    /// (the upcoming ApiService, Storefront, Admin Blazor apps) should call this
    /// extension explicitly so wiring is statically traceable.
    /// </summary>
    public static class CoreModuleExtensions
    {
        public static IServiceCollection AddCoreModule(this IServiceCollection services)
        {
            services.AddTransient<IEntityService, EntityService>();
            services.AddTransient<IMediaService, MediaService>();
            services.AddTransient<IThemeService, ThemeService>();
            services.AddTransient<IWidgetInstanceService, WidgetInstanceService>();
            services.AddScoped<IWorkContext, WorkContext>();
            services.AddScoped<ISmsSender, SmsSender>();
            services.AddSingleton<SettingDefinitionProvider>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<ICurrencyService, CurrencyService>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.core");

            return services;
        }
    }
}

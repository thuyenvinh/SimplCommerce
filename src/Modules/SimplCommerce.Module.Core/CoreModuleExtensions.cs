using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.Core
{
    public static class CoreModuleExtensions
    {
        public static IServiceCollection AddCoreModule(this IServiceCollection services)
        {
            services.AddTransient<IEntityService, EntityService>();
            services.AddTransient<IMediaService, MediaService>();
            services.AddTransient<IWidgetInstanceService, WidgetInstanceService>();
            services.AddScoped<IWorkContext, WorkContext>();
            services.AddScoped<ISmsSender, SmsSender>();
            services.AddSingleton<SettingDefinitionProvider>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<ICurrencyService, CurrencyService>();
            return services;
        }
    }
}

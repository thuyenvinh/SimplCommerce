using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.StorageLocal
{
    public static class StorageLocalModuleExtensions
    {
        public static IServiceCollection AddStorageLocalModule(this IServiceCollection services)
        {
            services.AddSingleton<IStorageService, LocalStorageService>();
            return services;
        }
    }
}

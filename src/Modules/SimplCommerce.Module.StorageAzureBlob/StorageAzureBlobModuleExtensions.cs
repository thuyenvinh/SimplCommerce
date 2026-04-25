using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.StorageAzureBlob
{
    public static class StorageAzureBlobModuleExtensions
    {
        public static IServiceCollection AddStorageAzureBlobModule(this IServiceCollection services)
        {
            services.AddSingleton<IStorageService, AzureBlobStorageService>();
            return services;
        }
    }
}

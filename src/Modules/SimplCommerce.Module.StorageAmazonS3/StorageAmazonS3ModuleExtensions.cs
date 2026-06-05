using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.StorageAmazonS3
{
    public static class StorageAmazonS3ModuleExtensions
    {
        public static IServiceCollection AddStorageAmazonS3Module(this IServiceCollection services)
        {
            services.AddSingleton<IStorageService, S3StorageService>();
            return services;
        }
    }
}

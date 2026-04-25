using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.SampleData.Data;
using SimplCommerce.Module.SampleData.Services;

namespace SimplCommerce.Module.SampleData
{
    public static class SampleDataModuleExtensions
    {
        public static IServiceCollection AddSampleDataModule(this IServiceCollection services)
        {
            services.AddTransient<ISqlRepository, SqlRepository>();
            services.AddTransient<ISampleDataService, SampleDataService>();
            return services;
        }
    }
}

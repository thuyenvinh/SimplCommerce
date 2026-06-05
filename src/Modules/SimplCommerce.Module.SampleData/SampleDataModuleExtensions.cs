using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.SampleData.Data;

namespace SimplCommerce.Module.SampleData
{
    public static class SampleDataModuleExtensions
    {
        public static IServiceCollection AddSampleDataModule(this IServiceCollection services)
        {
            services.AddTransient<ISqlRepository, SqlRepository>();
            return services;
        }
    }
}

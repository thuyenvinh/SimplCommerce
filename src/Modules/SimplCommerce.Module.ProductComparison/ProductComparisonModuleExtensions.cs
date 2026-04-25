using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.ProductComparison.Services;

namespace SimplCommerce.Module.ProductComparison
{
    public static class ProductComparisonModuleExtensions
    {
        public static IServiceCollection AddProductComparisonModule(this IServiceCollection services)
        {
            services.AddTransient<IComparingProductService, ComparingProductService>();
            return services;
        }
    }
}

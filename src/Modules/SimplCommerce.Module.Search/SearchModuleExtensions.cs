using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.Search
{
    public static class SearchModuleExtensions
    {
        public static IServiceCollection AddSearchModule(this IServiceCollection services)
        {
            GlobalConfiguration.RegisterAngularModule("simplAdmin.search");
            return services;
        }
    }
}

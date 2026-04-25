using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Tax.Services;

namespace SimplCommerce.Module.Tax
{
    public static class TaxModuleExtensions
    {
        public static IServiceCollection AddTaxModule(this IServiceCollection services)
        {
            services.AddTransient<ITaxService, TaxService>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.tax");
            return services;
        }
    }
}

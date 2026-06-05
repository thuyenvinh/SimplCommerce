using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Vendors.Services;

namespace SimplCommerce.Module.Vendors
{
    public static class VendorsModuleExtensions
    {
        public static IServiceCollection AddVendorsModule(this IServiceCollection services)
        {
            services.AddTransient<IVendorService, VendorService>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.vendors");
            return services;
        }
    }
}

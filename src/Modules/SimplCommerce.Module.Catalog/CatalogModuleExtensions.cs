using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Catalog.Data;
using SimplCommerce.Module.Catalog.Events;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Events;

namespace SimplCommerce.Module.Catalog
{
    public static class CatalogModuleExtensions
    {
        public static IServiceCollection AddCatalogModule(this IServiceCollection services)
        {
            services.AddTransient<IProductTemplateProductAttributeRepository, ProductTemplateProductAttributeRepository>();
            services.AddTransient<INotificationHandler<ReviewSummaryChanged>, ReviewSummaryChangedHandler>();
            services.AddTransient<IBrandService, BrandService>();
            services.AddTransient<IProductPricingService, ProductPricingService>();
            services.AddTransient<IProductService, ProductService>();
            return services;
        }
    }
}

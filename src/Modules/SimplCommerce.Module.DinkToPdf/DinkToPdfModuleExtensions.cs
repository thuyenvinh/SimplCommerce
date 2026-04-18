using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.DinkToPdf
{
    public static class DinkToPdfModuleExtensions
    {
        public static IServiceCollection AddDinkToPdfModule(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
            services.AddTransient<IPdfConverter, DinkToPdfConverter>();
            return services;
        }
    }
}

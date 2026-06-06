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
            // Lazy factory rather than an eager instance: `new PdfTools()` loads the
            // native libwkhtmltox at construction, and its finalizer P/Invokes
            // wkhtmltopdf_deinit() — which crashes the whole process at GC on any host
            // missing the native lib (e.g. the integration-test host). Deferring to a
            // factory means PdfTools is only built when IConverter is first resolved,
            // i.e. when a PDF is actually generated.
            services.AddSingleton<IConverter>(_ => new SynchronizedConverter(new PdfTools()));
            services.AddTransient<IPdfConverter, DinkToPdfConverter>();
            return services;
        }
    }
}

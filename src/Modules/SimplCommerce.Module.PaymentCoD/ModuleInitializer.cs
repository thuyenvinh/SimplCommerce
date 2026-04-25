using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure.Modules;

namespace SimplCommerce.Module.PaymentCoD
{
    [Obsolete("Call services.AddPaymentCoDModule() in your composition root.")]
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services) =>
            services.AddPaymentCoDModule();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}

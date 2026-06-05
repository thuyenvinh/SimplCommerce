using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure.Modules;

namespace SimplCommerce.Module.HangfireJobs
{
    [Obsolete("Call services.AddHangfireJobsModule(configuration) and app.UseHangfireJobsModule() in your composition root.")]
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Legacy shim: the reflection path has no configuration available, so resolve it
            // from the temporary provider — preserves the pre-Phase-2 behaviour exactly.
            using var sp = services.BuildServiceProvider();
            var configuration = sp.GetRequiredService<IConfiguration>();
            services.AddHangfireJobsModule(configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) =>
            app.UseHangfireJobsModule();
    }
}

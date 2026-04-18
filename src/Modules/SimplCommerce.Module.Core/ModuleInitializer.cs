using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure.Modules;

namespace SimplCommerce.Module.Core
{
    /// <summary>
    /// Backward-compatibility shim for the reflection-driven module loader. New code should call
    /// <see cref="CoreModuleExtensions.AddCoreModule"/> directly from the host's composition root.
    /// </summary>
    [Obsolete("Call services.AddCoreModule() in your composition root. ModuleInitializer is kept only for the legacy ConfigureModules() reflection scan and will be removed once all hosts wire modules explicitly.")]
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection) =>
            serviceCollection.AddCoreModule();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}

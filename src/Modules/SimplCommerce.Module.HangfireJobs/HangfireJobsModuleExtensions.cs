using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.HangfireJobs.Extensions;
using SimplCommerce.Module.HangfireJobs.Internal;

namespace SimplCommerce.Module.HangfireJobs
{
    public static class HangfireJobsModuleExtensions
    {
        public static IServiceCollection AddHangfireJobsModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SimplCommerce")
                ?? configuration.GetConnectionString("DefaultConnection");

            services.AddHangfireService(config =>
            {
                config.UseSqlServerStorage(connectionString);
            });

            services.PostConfigure<HangfireConfigureOptions>(o =>
            {
                o.Dasbhoard.AuthorizationCallback = httpContext =>
                {
                    var user = httpContext.User;
                    return user.Identity!.IsAuthenticated && user.IsInRole("admin");
                };
            });

            return services;
        }

        /// <summary>
        /// Must be called AFTER UseRouting() in the composition root so the Hangfire
        /// dashboard endpoint is part of endpoint routing.
        /// </summary>
        public static IApplicationBuilder UseHangfireJobsModule(this IApplicationBuilder app)
        {
            app.UseHangfire();
            app.InitializeHangfireJobs();
            return app;
        }
    }
}

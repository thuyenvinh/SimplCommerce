using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.ActivityLog.Data;
using SimplCommerce.Module.ActivityLog.Events;
using SimplCommerce.Module.Core.Events;

namespace SimplCommerce.Module.ActivityLog
{
    public static class ActivityLogModuleExtensions
    {
        public static IServiceCollection AddActivityLogModule(this IServiceCollection services)
        {
            services.AddTransient<IActivityTypeRepository, ActivityRepository>();
            services.AddTransient<INotificationHandler<EntityViewed>, EntityViewedHandler>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.activityLog");
            return services;
        }
    }
}

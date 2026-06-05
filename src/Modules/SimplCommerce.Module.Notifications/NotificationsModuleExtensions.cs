using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Module.Core.Events;
using SimplCommerce.Module.Notifications.Data;
using SimplCommerce.Module.Notifications.Events;
using SimplCommerce.Module.Notifications.Jobs;
using SimplCommerce.Module.Notifications.Notifiers;
using SimplCommerce.Module.SignalR.RealTime;
// Alias disambiguates the module's notification publisher from MediatR's
// INotificationPublisher (added in MediatR 12 for PublishStrategy customisation).
using NotificationServices = SimplCommerce.Module.Notifications.Services;

namespace SimplCommerce.Module.Notifications
{
    public static class NotificationsModuleExtensions
    {
        public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
        {
            services.AddTransient<INotificationRepository, NotificationRepository>();

            services.AddSingleton<NotificationServices.INotificationDefinitionManager, NotificationServices.NotificationDefinitionManager>();
            services.AddSingleton<IOnlineClientManager, OnlineClientManager>();
            services.AddSingleton<NotificationServices.IRealTimeNotifier, NotificationServices.SignalRRealTimeNotifier>();

            services.AddTransient<NotificationServices.INotificationDistributer, NotificationServices.NotificationDistributer>();
            services.AddTransient<NotificationServices.INotificationPublisher, NotificationServices.NotificationPublisher>();
            services.AddTransient<NotificationServices.INotificationSubscriptionManager, NotificationServices.NotificationSubscriptionManager>();
            services.AddTransient<NotificationServices.IUserNotificationManager, NotificationServices.UserNotificationManager>();

            services.AddTransient<ITestNotifier, TestNotifier>();
            services.AddTransient<INotificationHandler<UserSignedIn>, UserSignedInHandler>();
            services.AddTransient<NotificationDistributionJob>();

            return services;
        }
    }
}

using System;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Core.Events;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Localization.Events;
using SimplCommerce.Module.Localization.Services;

namespace SimplCommerce.Module.Localization
{
    public static class LocalizationModuleExtensions
    {
        public static IServiceCollection AddLocalizationModule(this IServiceCollection services)
        {
            services.AddTransient<INotificationHandler<UserSignedIn>, UserSignedInHandler>();
            services.AddTransient<IContentLocalizationService, ContentLocalizationService>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.localization");
            return services;
        }
    }
}

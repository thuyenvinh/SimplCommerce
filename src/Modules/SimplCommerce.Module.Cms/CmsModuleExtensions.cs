using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Cms.Events;
using SimplCommerce.Module.Cms.Services;
using SimplCommerce.Module.Core.Events;

namespace SimplCommerce.Module.Cms
{
    public static class CmsModuleExtensions
    {
        public static IServiceCollection AddCmsModule(this IServiceCollection services)
        {
            services.AddTransient<INotificationHandler<EntityDeleting>, EntityDeletingHandler>();
            services.AddTransient<IPageService, PageService>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.cms");
            return services;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.News.Services;

namespace SimplCommerce.Module.News
{
    public static class NewsModuleExtensions
    {
        public static IServiceCollection AddNewsModule(this IServiceCollection services)
        {
            services.AddTransient<INewsItemService, NewsItemService>();
            services.AddTransient<INewsCategoryService, NewsCategoryService>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.news");
            return services;
        }
    }
}

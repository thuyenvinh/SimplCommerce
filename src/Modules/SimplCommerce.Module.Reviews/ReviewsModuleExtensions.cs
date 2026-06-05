using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Reviews.Data;

namespace SimplCommerce.Module.Reviews
{
    public static class ReviewsModuleExtensions
    {
        public static IServiceCollection AddReviewsModule(this IServiceCollection services)
        {
            services.AddTransient<IReplyRepository, ReplyRepository>();
            services.AddTransient<IReviewRepository, ReviewRepository>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.reviews");
            return services;
        }
    }
}

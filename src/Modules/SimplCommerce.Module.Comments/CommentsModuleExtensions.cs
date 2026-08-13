using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;
using SimplCommerce.Module.Comments.Data;

namespace SimplCommerce.Module.Comments
{
    public static class CommentsModuleExtensions
    {
        public static IServiceCollection AddCommentsModule(this IServiceCollection services)
        {
            services.AddTransient<ICommentRepository, CommentRepository>();
            return services;
        }
    }
}

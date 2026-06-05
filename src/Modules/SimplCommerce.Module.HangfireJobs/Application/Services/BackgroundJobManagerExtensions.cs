using System;
using SimplCommerce.Module.HangfireJobs.Services;

namespace SimplCommerce.Module.HangfireJobs.Extensions
{
    public static class BackgroundJobManagerExtensions
    {
        public static string Enqueue<TArgs>(this IBackgroundJobManager backgroundJobManager, TArgs args, TimeSpan? delay = null)
        {
            return backgroundJobManager.EnqueueAsync(args, delay).GetAwaiter().GetResult();
        }
    }
}

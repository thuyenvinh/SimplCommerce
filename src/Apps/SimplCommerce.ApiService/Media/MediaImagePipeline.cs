using Microsoft.Extensions.FileProviders;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Commands;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Processors;
using SixLabors.ImageSharp.Web.Providers;

namespace SimplCommerce.ApiService.Media;

/// <summary>
/// ImageSharp.Web pipeline over the local <c>user-content/</c> directory:
/// <c>/user-content/{file}?width=&amp;height=&amp;rmode=&amp;format=webp&amp;quality=</c>.
/// Disk-cached resized variants live under <c>is-cache/</c> relative to WebRootPath.
///
/// Safety rails:
/// - The command parser only trusts a known whitelist (<c>width</c>, <c>height</c>, <c>rmode</c>,
///   <c>format</c>, <c>quality</c>) so callers can't bomb the server with arbitrary transforms.
/// - Max size 4000x4000; quality clamped to 10..90.
/// - Cache lifetime 30 days — varies on command string, so each (w,h,fmt,q) tuple is its own file.
/// </summary>
public static class MediaImagePipeline
{
    private const string MediaRoot = "user-content";

    public static IServiceCollection AddMediaImagePipeline(this IServiceCollection services)
    {
        services.AddImageSharp(options =>
        {
            options.BrowserMaxAge = TimeSpan.FromDays(7);
            options.CacheMaxAge = TimeSpan.FromDays(30);
            options.OnParseCommandsAsync = ctx =>
            {
                SanitizeCommands(ctx.Commands);
                return Task.CompletedTask;
            };
        })
        .Configure<PhysicalFileSystemProviderOptions>(options =>
        {
            options.ProviderRootPath = null;
        })
        .SetRequestParser<QueryCollectionRequestParser>()
        .Configure<PhysicalFileSystemCacheOptions>(options =>
        {
            options.CacheFolder = "is-cache";
        })
        .SetCache<PhysicalFileSystemCache>()
        .SetCacheKey<UriRelativeLowerInvariantCacheKey>()
        .SetCacheHash<SHA256CacheHash>()
        .AddProvider<PhysicalFileSystemProvider>();

        return services;
    }

    public static IApplicationBuilder UseMediaImagePipeline(this WebApplication app)
    {
        var mediaPath = Path.Combine(app.Environment.WebRootPath ?? app.Environment.ContentRootPath, MediaRoot);
        Directory.CreateDirectory(mediaPath);

        app.UseImageSharp();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(mediaPath),
            RequestPath = $"/{MediaRoot}",
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800";
            }
        });
        return app;
    }

    private static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        ResizeWebProcessor.Width, ResizeWebProcessor.Height,
        ResizeWebProcessor.Mode, "format", "quality"
    };

    public static void SanitizeCommands(CommandCollection commands)
    {
        ClampInt(commands, ResizeWebProcessor.Width, 1, 4000);
        ClampInt(commands, ResizeWebProcessor.Height, 1, 4000);
        ClampInt(commands, "quality", 10, 90);
        foreach (var key in commands.Keys.Where(k => !AllowedCommands.Contains(k)).ToList())
        {
            commands.Remove(key);
        }
    }

    private static void ClampInt(CommandCollection commands, string key, int min, int max)
    {
        if (commands.TryGetValue(key, out var raw) &&
            int.TryParse(raw, out var value))
        {
            commands[key] = Math.Clamp(value, min, max).ToString();
        }
    }
}

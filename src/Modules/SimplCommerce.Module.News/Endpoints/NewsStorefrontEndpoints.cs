#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.News.Models;

namespace SimplCommerce.Module.News.Endpoints;

public static class NewsStorefrontEndpoints
{
    public record NewsItemSummary(long Id, string Name, string Slug, string? ShortContent);
    public record NewsItemDetail(long Id, string Name, string Slug, string? ShortContent, string? FullContent);
    public record NewsCategoryItem(long Id, string Name, string Slug);

    public static IEndpointRouteBuilder MapNewsStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/news").WithTags("Storefront.News");

        group.MapGet("/", async (IRepository<NewsItem> repo, int page = 1, int pageSize = 12) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 50);
            var items = await repo.Query()
                .Where(n => !n.IsDeleted && n.IsPublished)
                .OrderByDescending(n => n.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(n => new NewsItemSummary(n.Id, n.Name, n.Slug, n.ShortContent))
                .ToListAsync();
            return Results.Ok(items);
        });

        group.MapGet("/{slug}", async (string slug, IRepository<NewsItem> repo) =>
        {
            var item = await repo.Query()
                .Where(n => !n.IsDeleted && n.IsPublished && n.Slug == slug)
                .Select(n => new NewsItemDetail(n.Id, n.Name, n.Slug, n.ShortContent, n.FullContent))
                .FirstOrDefaultAsync();
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapGet("/categories", async (IRepository<NewsCategory> repo) =>
        {
            var categories = await repo.Query()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new NewsCategoryItem(c.Id, c.Name, c.Slug))
                .ToListAsync();
            return Results.Ok(categories);
        });

        return app;
    }
}

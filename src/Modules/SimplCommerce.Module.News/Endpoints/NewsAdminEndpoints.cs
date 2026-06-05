#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.News.Models;

namespace SimplCommerce.Module.News.Endpoints;

public static class NewsAdminEndpoints
{
    public record NewsItemInput(string Name, string Slug, string? ShortContent, string? FullContent, bool IsPublished);
    public record NewsItemDetail(long Id, string Name, string Slug, string? ShortContent, string? FullContent, bool IsPublished, System.DateTimeOffset CreatedOn);
    public record NewsCategoryInput(string Name, string Slug, bool IsPublished);
    public record NewsCategoryItem(long Id, string Name, string Slug, bool IsPublished);

    public static IEndpointRouteBuilder MapNewsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/news")
            .WithTags("Admin.News")
            .RequireAuthorization("AdminOnly");

        // --- News items ---
        group.MapGet("/items", async (IRepository<NewsItem> repo, int page = 1, int pageSize = 20) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var query = repo.Query().Where(n => !n.IsDeleted);
            var total = await query.CountAsync();
            var rows = await query
                .OrderByDescending(n => n.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(n => new { n.Id, n.Name, n.Slug, n.IsPublished, n.CreatedOn })
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        group.MapGet("/items/{id:long}", async (long id, IRepository<NewsItem> repo) =>
        {
            var item = await repo.Query().FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            return item is null
                ? Results.NotFound()
                : Results.Ok(new NewsItemDetail(item.Id, item.Name, item.Slug, item.ShortContent, item.FullContent, item.IsPublished, item.CreatedOn));
        });

        group.MapPost("/items", async (NewsItemInput input, IRepository<NewsItem> repo) =>
        {
            var item = new NewsItem
            {
                Name = input.Name, Slug = input.Slug,
                ShortContent = input.ShortContent ?? string.Empty,
                FullContent = input.FullContent ?? string.Empty,
                IsPublished = input.IsPublished,
            };
            repo.Add(item);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/news/items/{item.Id}", new { item.Id });
        });

        group.MapPut("/items/{id:long}", async (long id, NewsItemInput input, IRepository<NewsItem> repo) =>
        {
            var item = await repo.Query().FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            if (item is null) return Results.NotFound();
            item.Name = input.Name;
            item.Slug = input.Slug;
            item.ShortContent = input.ShortContent ?? string.Empty;
            item.FullContent = input.FullContent ?? string.Empty;
            item.IsPublished = input.IsPublished;
            item.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/items/{id:long}", async (long id, IRepository<NewsItem> repo) =>
        {
            var item = await repo.Query().FirstOrDefaultAsync(n => n.Id == id);
            if (item is null) return Results.NotFound();
            item.IsDeleted = true;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        // --- News categories ---
        group.MapGet("/categories", async (IRepository<NewsCategory> repo) =>
        {
            var list = await repo.Query().Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new NewsCategoryItem(c.Id, c.Name, c.Slug, c.IsPublished))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapPost("/categories", async (NewsCategoryInput input, IRepository<NewsCategory> repo) =>
        {
            var cat = new NewsCategory { Name = input.Name, Slug = input.Slug, IsPublished = input.IsPublished };
            repo.Add(cat);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/news/categories/{cat.Id}", new { cat.Id });
        });

        group.MapPut("/categories/{id:long}", async (long id, NewsCategoryInput input, IRepository<NewsCategory> repo) =>
        {
            var cat = await repo.Query().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (cat is null) return Results.NotFound();
            cat.Name = input.Name;
            cat.Slug = input.Slug;
            cat.IsPublished = input.IsPublished;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/categories/{id:long}", async (long id, IRepository<NewsCategory> repo) =>
        {
            var cat = await repo.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (cat is null) return Results.NotFound();
            cat.IsDeleted = true;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

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
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.Search.Endpoints;

public static class SearchStorefrontEndpoints
{
    public record SearchResult(long Id, string Name, string Slug, decimal Price, decimal? OldPrice, string? ThumbnailUrl);
    public record SearchResponse(IReadOnlyList<SearchResult> Items, int TotalCount, string Query);

    public static IEndpointRouteBuilder MapSearchStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/search").WithTags("Storefront.Search");

        group.MapGet("/", async (
            IRepository<Product> repository,
            IProductPricingService pricing,
            IMediaService media,
            string q,
            int page = 1,
            int pageSize = 24) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return TypedResults.Ok(new SearchResponse([], 0, string.Empty));
            }

            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var pattern = $"%{q.Trim()}%";

            var query = repository.Query()
                .Include(p => p.ThumbnailImage)
                .Where(p => p.IsPublished && p.IsVisibleIndividually)
                .Where(p => EF.Functions.Like(p.Name, pattern)
                    || EF.Functions.Like(p.ShortDescription, pattern));

            var total = await query.CountAsync();
            var rows = await query
                .OrderByDescending(p => p.CreatedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var results = rows.Select(p =>
            {
                var calc = pricing.CalculateProductPrice(p);
                return new SearchResult(p.Id, p.Name, p.Slug, calc.Price, calc.OldPrice,
                    media.GetThumbnailUrl(p.ThumbnailImage));
            }).ToList();

            return TypedResults.Ok(new SearchResponse(results, total, q));
        });

        return app;
    }
}

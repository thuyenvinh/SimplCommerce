#nullable enable
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
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.Vendors.Endpoints;

/// <summary>
/// Wave 10: public vendor pages.
/// • GET /api/storefront/vendors                  — list active+published vendors
/// • GET /api/storefront/vendors/{slug}           — detail of a vendor
/// • GET /api/storefront/vendors/{slug}/products  — vendor's published catalog
///
/// All endpoints are anonymous (no auth). They respect the same publication
/// gates as the storefront catalog (IsPublished, IsAllowToOrder, IsDeleted).
/// </summary>
public static class VendorsStorefrontEndpoints
{
    public record StorefrontVendorItem(long Id, string Name, string Slug, string? Description);
    public record StorefrontVendorDetail(long Id, string Name, string Slug, string? Description);
    public record StorefrontVendorProductItem(
        long Id, string Name, string Slug, decimal Price, decimal? OldPrice,
        string? ThumbnailUrl, bool IsCallForPricing, bool IsAllowToOrder,
        double? RatingAverage, int ReviewsCount);
    public record StorefrontVendorProductsPage(int Total, int Page, int PageSize, System.Collections.Generic.IReadOnlyList<StorefrontVendorProductItem> Items);

    public static IEndpointRouteBuilder MapVendorsStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/vendors").WithTags("Storefront.Vendors");

        group.MapGet("/", async (IRepository<Vendor> repo) =>
        {
            var rows = await repo.Query()
                .Where(v => !v.IsDeleted && v.IsActive)
                .OrderBy(v => v.Name)
                .Select(v => new StorefrontVendorItem(v.Id, v.Name, v.Slug, v.Description))
                .ToListAsync();
            return Results.Ok(rows);
        });

        group.MapGet("/{slug}", async (string slug, IRepository<Vendor> repo) =>
        {
            var v = await repo.Query()
                .Where(x => !x.IsDeleted && x.IsActive && x.Slug == slug)
                .Select(x => new StorefrontVendorDetail(x.Id, x.Name, x.Slug, x.Description))
                .FirstOrDefaultAsync();
            return v is null ? Results.NotFound() : Results.Ok(v);
        });

        group.MapGet("/{slug}/products", async (
            string slug,
            IRepository<Vendor> vendors,
            IRepository<Product> products,
            IProductPricingService pricing,
            IMediaService media,
            int page = 1,
            int pageSize = 24) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var vendor = await vendors.Query()
                .Where(v => !v.IsDeleted && v.IsActive && v.Slug == slug)
                .Select(v => new { v.Id })
                .FirstOrDefaultAsync();
            if (vendor is null) return Results.NotFound();

            // Mirror CatalogStorefront ListProductsAsync gating (G11/G12 from
            // Wave 1): only published + visible-individually + allow-to-order
            // products show up on a vendor's public storefront.
            var query = products.Query()
                .Include(p => p.ThumbnailImage)
                .Where(p => p.VendorId == vendor.Id
                    && !p.IsDeleted
                    && p.IsPublished
                    && p.IsVisibleIndividually
                    && p.IsAllowToOrder);
            var total = await query.CountAsync();
            var rows = await query.OrderByDescending(p => p.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();
            var items = rows.Select(p =>
            {
                var calc = pricing.CalculateProductPrice(p);
                return new StorefrontVendorProductItem(
                    p.Id, p.Name, p.Slug, calc.Price, calc.OldPrice,
                    media.GetThumbnailUrl(p.ThumbnailImage),
                    p.IsCallForPricing, p.IsAllowToOrder,
                    p.RatingAverage, p.ReviewsCount);
            }).ToList();
            return Results.Ok(new StorefrontVendorProductsPage(total, page, pageSize, items));
        });

        return app;
    }
}

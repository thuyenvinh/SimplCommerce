#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Infrastructure.Web;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;

namespace SimplCommerce.Module.Catalog.Endpoints;

/// <summary>
/// Admin-facing Catalog API. Replaces AngularJS admin routes like /api/products,
/// /api/categories, /api/brands. This group covers the core CRUD needs —
/// product-attribute, product-option, product-template etc. are follow-ups.
/// </summary>
public static class CatalogAdminEndpoints
{
    public record BrandInput(string Name, string Slug, bool IsPublished);
    public record CategoryInput(string Name, string Slug, long? ParentId, int DisplayOrder, string? Description);

    public record ProductInput(
        string Name, string Slug, string? Sku,
        decimal Price, decimal? OldPrice,
        string? ShortDescription, string? Description, string? Specification,
        bool IsPublished, bool IsAllowToOrder, bool IsCallForPricing, bool IsFeatured,
        bool StockTrackingIsEnabled, int StockQuantity,
        long? BrandId,
        // G08: round-trip CategoryIds so PUT can move a product between categories
        // (read by ProductEditDto on GET). Null = don't touch; empty list = clear.
        System.Collections.Generic.IReadOnlyList<long>? CategoryIds = null);

    public record ProductEditDto(
        long Id, string Name, string Slug, string? Sku,
        decimal Price, decimal? OldPrice,
        string? ShortDescription, string? Description, string? Specification,
        bool IsPublished, bool IsAllowToOrder, bool IsCallForPricing, bool IsFeatured,
        bool StockTrackingIsEnabled, int StockQuantity,
        long? BrandId, System.Collections.Generic.IReadOnlyList<long> CategoryIds);

    // G07: variant CRUD. Variants are sibling Product rows with IsVisibleIndividually=false,
    // joined back to the parent through ProductLink(LinkType=Super). Only fields that
    // legitimately vary per-SKU are surfaced — name/sku/price/stock/published — so the
    // parent's marketing copy (description, brand, categories) stays the single source.
    public record ProductVariantInput(
        string Name, string? Sku, decimal Price, decimal? OldPrice,
        int StockQuantity, bool IsPublished);

    public record ProductVariantDto(
        long Id, string Name, string Slug, string? Sku,
        decimal Price, decimal? OldPrice, int StockQuantity, bool IsPublished);

    public static IEndpointRouteBuilder MapCatalogAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/catalog")
            .WithTags("Admin.Catalog")
            .RequireAuthorization("AdminOrVendor");

        // ---- Brands ----
        group.MapGet("/brands", async (IRepository<Brand> repo) =>
        {
            var list = await repo.Query().Where(b => !b.IsDeleted)
                .Select(b => new { b.Id, b.Name, b.Slug, b.IsPublished }).ToListAsync();
            return Results.Ok(list);
        });

        // Wave 6: brand mutations are platform-level. Vendors can READ the brand
        // list (GET above is allowed for AdminOrVendor) but can't create/edit/delete —
        // brands are a shared taxonomy maintained by platform admins.
        group.MapPost("/brands", (BrandInput input, IRepository<Brand> repo) =>
        {
            var brand = new Brand { Name = input.Name, Slug = input.Slug, IsPublished = input.IsPublished };
            repo.Add(brand);
            repo.SaveChanges();
            return Results.Created($"/api/admin/catalog/brands/{brand.Id}", new { brand.Id });
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/brands/{id:long}", async (long id, BrandInput input, IRepository<Brand> repo) =>
        {
            var brand = await repo.Query().FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (brand is null) return Results.NotFound();
            brand.Name = input.Name;
            brand.Slug = input.Slug;
            brand.IsPublished = input.IsPublished;
            repo.SaveChanges();
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/brands/{id:long}", async (long id, IRepository<Brand> repo) =>
        {
            var brand = await repo.Query().FirstOrDefaultAsync(b => b.Id == id);
            if (brand is null) return Results.NotFound();
            brand.IsDeleted = true;
            repo.SaveChanges();
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        // ---- Categories ----
        group.MapGet("/categories", async (IRepository<Category> repo) =>
        {
            var list = await repo.Query().Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new { c.Id, c.Name, c.Slug, c.ParentId, c.DisplayOrder, c.Description })
                .ToListAsync();
            return Results.Ok(list);
        });

        // Wave 6: categories likewise are shared taxonomy — platform-admin only for writes.
        group.MapPost("/categories", (CategoryInput input, IRepository<Category> repo) =>
        {
            var category = new Category
            {
                Name = input.Name,
                Slug = input.Slug,
                ParentId = input.ParentId,
                DisplayOrder = input.DisplayOrder,
                Description = input.Description ?? string.Empty,
            };
            repo.Add(category);
            repo.SaveChanges();
            return Results.Created($"/api/admin/catalog/categories/{category.Id}", new { category.Id });
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/categories/{id:long}", async (long id, CategoryInput input, IRepository<Category> repo) =>
        {
            var category = await repo.Query().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category is null) return Results.NotFound();
            category.Name = input.Name;
            category.Slug = input.Slug;
            category.ParentId = input.ParentId;
            category.DisplayOrder = input.DisplayOrder;
            category.Description = input.Description ?? string.Empty;
            repo.SaveChanges();
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/categories/{id:long}", async (long id, IRepository<Category> repo) =>
        {
            var category = await repo.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (category is null) return Results.NotFound();
            category.IsDeleted = true;
            repo.SaveChanges();
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        // ---- Products ----
        // Wave 6: vendor-scoped reads. When the caller's JWT has vendor_id, every
        // product query filters down to that vendor's catalog so vendors can't see
        // (or mutate) each other's SKUs. Admin users (no vendor_id claim) keep the
        // global view.
        group.MapGet("/products", async (IRepository<Product> repo, IVendorScope scope, int page = 1, int pageSize = 20, string? search = null) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var query = repo.Query().Where(p => !p.IsDeleted);
            if (scope.CurrentVendorId is { } vid) query = query.Where(p => p.VendorId == vid);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(p => EF.Functions.Like(p.Name, pattern) || EF.Functions.Like(p.Sku, pattern));
            }
            var total = await query.CountAsync();
            var rows = await query.OrderByDescending(p => p.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new
                {
                    p.Id, p.Name, p.Slug, p.Sku, p.Price, p.OldPrice, p.StockQuantity,
                    p.IsPublished, p.IsAllowToOrder, p.CreatedOn
                })
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        group.MapGet("/products/{id:long}", async (long id, IRepository<Product> repo, IVendorScope scope) =>
        {
            var product = await repo.Query()
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product is null) return Results.NotFound();
            // Vendor scope returns 404 (not 403) for cross-vendor access so we
            // don't leak the existence of other vendors' SKU ids.
            if (scope.CurrentVendorId is { } vid && product.VendorId != vid) return Results.NotFound();
            var dto = new ProductEditDto(
                product.Id, product.Name, product.Slug, product.Sku,
                product.Price, product.OldPrice,
                product.ShortDescription, product.Description, product.Specification,
                product.IsPublished, product.IsAllowToOrder, product.IsCallForPricing, product.IsFeatured,
                product.StockTrackingIsEnabled, product.StockQuantity,
                product.BrandId,
                product.Categories.Select(c => c.CategoryId).ToList());
            return Results.Ok(dto);
        });

        group.MapPost("/products", async (ProductInput input, IRepository<Product> repo, IVendorScope scope, System.Security.Claims.ClaimsPrincipal principal) =>
        {
            if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Slug))
            {
                return Results.BadRequest(new { error = "Name and slug are required." });
            }
            // Stamp CreatedById from the JWT sub so SQLite/SQL Server FK checks
            // pass. Falling back to 0 was tolerated by SQL Server seed data (no FK)
            // but rejected by Sqlite's strict FK enforcement and is wrong either
            // way — the audit trail wants a real user id.
            var rawUserId = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            long.TryParse(rawUserId, out var actingUserId);

            var product = new Product
            {
                Name = input.Name, Slug = input.Slug, Sku = input.Sku,
                Price = input.Price, OldPrice = input.OldPrice,
                ShortDescription = input.ShortDescription,
                Description = input.Description,
                Specification = input.Specification,
                IsPublished = input.IsPublished,
                IsAllowToOrder = input.IsAllowToOrder,
                IsCallForPricing = input.IsCallForPricing,
                IsFeatured = input.IsFeatured,
                StockTrackingIsEnabled = input.StockTrackingIsEnabled,
                StockQuantity = input.StockQuantity,
                BrandId = input.BrandId,
                IsVisibleIndividually = true,
                CreatedById = actingUserId,
                LatestUpdatedById = actingUserId,
                // Wave 6: stamp VendorId from the caller's scope. Admins creating
                // on behalf of a vendor must explicitly act-as via a future
                // X-Vendor-As header; for now admin-created products are platform
                // owned (VendorId null).
                VendorId = scope.CurrentVendorId,
            };
            if (input.CategoryIds is { Count: > 0 })
            {
                foreach (var cid in input.CategoryIds)
                {
                    product.AddCategory(new ProductCategory { CategoryId = cid });
                }
            }
            repo.Add(product);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/catalog/products/{product.Id}", new { product.Id });
        });

        group.MapPut("/products/{id:long}", async (long id, ProductInput input, IRepository<Product> repo, IVendorScope scope, System.Security.Claims.ClaimsPrincipal principal) =>
        {
            var product = await repo.Query()
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && product.VendorId != vid) return Results.NotFound();

            var rawUserId = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            long.TryParse(rawUserId, out var actingUserId);

            product.Name = input.Name;
            product.Slug = input.Slug;
            product.Sku = input.Sku;
            product.Price = input.Price;
            product.OldPrice = input.OldPrice;
            product.ShortDescription = input.ShortDescription;
            product.Description = input.Description;
            product.Specification = input.Specification;
            product.IsPublished = input.IsPublished;
            product.IsAllowToOrder = input.IsAllowToOrder;
            product.IsCallForPricing = input.IsCallForPricing;
            product.IsFeatured = input.IsFeatured;
            product.StockTrackingIsEnabled = input.StockTrackingIsEnabled;
            product.StockQuantity = input.StockQuantity;
            product.BrandId = input.BrandId;
            product.LatestUpdatedById = actingUserId;
            product.LatestUpdatedOn = System.DateTimeOffset.UtcNow;

            // G08: sync ProductCategory rows when the client sends an explicit list.
            // Null = leave categories untouched (backwards-compatible); empty = clear all.
            if (input.CategoryIds is not null)
            {
                var desired = input.CategoryIds.Distinct().ToHashSet();
                var existing = product.Categories.ToList();
                foreach (var stale in existing.Where(c => !desired.Contains(c.CategoryId)))
                {
                    product.Categories.Remove(stale);
                }
                var existingIds = existing.Select(c => c.CategoryId).ToHashSet();
                foreach (var add in desired.Where(id => !existingIds.Contains(id)))
                {
                    product.AddCategory(new ProductCategory { CategoryId = add });
                }
            }

            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/products/{id:long}", async (long id, IRepository<Product> repo, IVendorScope scope) =>
        {
            var product = await repo.Query().FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && product.VendorId != vid) return Results.NotFound();
            product.IsDeleted = true;
            repo.SaveChanges();
            return Results.NoContent();
        });

        // ---- Product variants (G07) ----
        // Variants live as child Product rows linked back to the parent via
        // ProductLink(LinkType=Super). HasOptions stays a manual admin field —
        // it's display-layer (the parent gets a swatch picker on the storefront)
        // and we don't want to flip it implicitly on the first variant insert.
        group.MapGet("/products/{parentId:long}/variants", async (long parentId, IRepository<Product> products, IVendorScope scope) =>
        {
            var parent = await products.Query().FirstOrDefaultAsync(p => p.Id == parentId && !p.IsDeleted);
            if (parent is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && parent.VendorId != vid) return Results.NotFound();
            var rows = await products.Query()
                .Where(v => !v.IsDeleted)
                .Join(products.Query()
                        .Where(p => p.Id == parentId)
                        .SelectMany(p => p.ProductLinks.Where(l => l.LinkType == ProductLinkType.Super)),
                    v => v.Id, l => l.LinkedProductId, (v, _) => v)
                .Select(v => new ProductVariantDto(v.Id, v.Name, v.Slug, v.Sku,
                    v.Price, v.OldPrice, v.StockQuantity, v.IsPublished))
                .ToListAsync();
            return Results.Ok(rows);
        });

        group.MapPost("/products/{parentId:long}/variants", async (long parentId, ProductVariantInput input, IRepository<Product> products, IVendorScope scope) =>
        {
            if (string.IsNullOrWhiteSpace(input.Name))
            {
                return Results.BadRequest(new { error = "Name is required." });
            }
            var parent = await products.Query().FirstOrDefaultAsync(p => p.Id == parentId && !p.IsDeleted);
            if (parent is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && parent.VendorId != vid) return Results.NotFound();

            var variant = new Product
            {
                Name = input.Name,
                // Slug must be globally unique on Product; derive from parent + name
                // so admins don't collide manually when adding "Red / Large" twice.
                Slug = $"{parent.Slug}-{System.Guid.NewGuid():N}".Substring(0, System.Math.Min(parent.Slug.Length + 9, 200)),
                Sku = input.Sku,
                Price = input.Price,
                OldPrice = input.OldPrice,
                StockQuantity = input.StockQuantity,
                IsPublished = input.IsPublished,
                IsVisibleIndividually = false,
                IsAllowToOrder = parent.IsAllowToOrder,
                IsCallForPricing = parent.IsCallForPricing,
                BrandId = parent.BrandId,
                TaxClassId = parent.TaxClassId,
                StockTrackingIsEnabled = parent.StockTrackingIsEnabled,
                // Variants inherit the parent's vendor so a future query filter is
                // consistent regardless of which row the join hits.
                VendorId = parent.VendorId,
            };
            parent.AddProductLinks(new ProductLink { LinkedProduct = variant, LinkType = ProductLinkType.Super });
            products.Add(variant);
            await products.SaveChangesAsync();
            return Results.Created($"/api/admin/catalog/products/{parentId}/variants/{variant.Id}", new { variant.Id });
        });

        group.MapPut("/products/{parentId:long}/variants/{variantId:long}", async (long parentId, long variantId, ProductVariantInput input, IRepository<Product> products, IVendorScope scope) =>
        {
            var variant = await products.Query()
                .Where(v => v.Id == variantId && !v.IsDeleted && !v.IsVisibleIndividually)
                .FirstOrDefaultAsync();
            if (variant is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && variant.VendorId != vid) return Results.NotFound();
            // Guard against cross-parent edits: only update if a Super link from
            // parentId actually points at this variant.
            var linked = await products.Query()
                .Where(p => p.Id == parentId)
                .SelectMany(p => p.ProductLinks)
                .AnyAsync(l => l.LinkedProductId == variantId && l.LinkType == ProductLinkType.Super);
            if (!linked) return Results.NotFound();

            variant.Name = input.Name;
            variant.Sku = input.Sku;
            variant.Price = input.Price;
            variant.OldPrice = input.OldPrice;
            variant.StockQuantity = input.StockQuantity;
            variant.IsPublished = input.IsPublished;
            variant.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
            await products.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/products/{parentId:long}/variants/{variantId:long}", async (long parentId, long variantId, IRepository<Product> products, IVendorScope scope) =>
        {
            var variant = await products.Query()
                .Where(v => v.Id == variantId && !v.IsVisibleIndividually)
                .FirstOrDefaultAsync();
            if (variant is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && variant.VendorId != vid) return Results.NotFound();
            var linked = await products.Query()
                .Where(p => p.Id == parentId)
                .SelectMany(p => p.ProductLinks)
                .AnyAsync(l => l.LinkedProductId == variantId && l.LinkType == ProductLinkType.Super);
            if (!linked) return Results.NotFound();

            // Soft-delete only — the historic order_item rows still reference this
            // product. Removing the ProductLink row would force a separate cascade
            // strategy; leave it intact and let the soft-delete filter hide the
            // variant everywhere it matters.
            variant.IsDeleted = true;
            await products.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

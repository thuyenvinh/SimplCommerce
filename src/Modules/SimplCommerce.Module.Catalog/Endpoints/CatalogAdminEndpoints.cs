#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
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

        group.MapPost("/brands", (BrandInput input, IRepository<Brand> repo) =>
        {
            var brand = new Brand { Name = input.Name, Slug = input.Slug, IsPublished = input.IsPublished };
            repo.Add(brand);
            repo.SaveChanges();
            return Results.Created($"/api/admin/catalog/brands/{brand.Id}", new { brand.Id });
        });

        group.MapPut("/brands/{id:long}", async (long id, BrandInput input, IRepository<Brand> repo) =>
        {
            var brand = await repo.Query().FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (brand is null) return Results.NotFound();
            brand.Name = input.Name;
            brand.Slug = input.Slug;
            brand.IsPublished = input.IsPublished;
            repo.SaveChanges();
            return Results.NoContent();
        });

        group.MapDelete("/brands/{id:long}", async (long id, IRepository<Brand> repo) =>
        {
            var brand = await repo.Query().FirstOrDefaultAsync(b => b.Id == id);
            if (brand is null) return Results.NotFound();
            brand.IsDeleted = true;
            repo.SaveChanges();
            return Results.NoContent();
        });

        // ---- Categories ----
        group.MapGet("/categories", async (IRepository<Category> repo) =>
        {
            var list = await repo.Query().Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new { c.Id, c.Name, c.Slug, c.ParentId, c.DisplayOrder, c.Description })
                .ToListAsync();
            return Results.Ok(list);
        });

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
        });

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
        });

        group.MapDelete("/categories/{id:long}", async (long id, IRepository<Category> repo) =>
        {
            var category = await repo.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (category is null) return Results.NotFound();
            category.IsDeleted = true;
            repo.SaveChanges();
            return Results.NoContent();
        });

        // ---- Products ----
        group.MapGet("/products", async (IRepository<Product> repo, int page = 1, int pageSize = 20, string? search = null) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var query = repo.Query().Where(p => !p.IsDeleted);
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

        group.MapGet("/products/{id:long}", async (long id, IRepository<Product> repo) =>
        {
            var product = await repo.Query()
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        group.MapDelete("/products/{id:long}", async (long id, IRepository<Product> repo) =>
        {
            var product = await repo.Query().FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return Results.NotFound();
            product.IsDeleted = true;
            repo.SaveChanges();
            return Results.NoContent();
        });

        return app;
    }
}

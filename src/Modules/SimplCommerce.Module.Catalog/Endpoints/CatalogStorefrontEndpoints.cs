#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

namespace SimplCommerce.Module.Catalog.Endpoints;

/// <summary>
/// Read-only storefront API for product catalog. Replaces the AngularJS-era
/// <c>/product/...</c> and <c>/brand/...</c> / <c>/category/...</c> MVC controllers with
/// JSON-first minimal endpoints. Admin CRUD lives in a separate admin endpoint group.
/// </summary>
public static class CatalogStorefrontEndpoints
{
    public static IEndpointRouteBuilder MapCatalogStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/catalog").WithTags("Storefront.Catalog");

        group.MapGet("/products", ListProductsAsync)
            .WithName("ListProducts")
            .WithSummary("List published products with paging + optional category/brand/search filter.");

        group.MapGet("/products/{id:long}", GetProductByIdAsync)
            .WithName("GetProductById");

        group.MapGet("/products/by-slug/{slug}", GetProductBySlugAsync)
            .WithName("GetProductBySlug");

        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListCategories");

        group.MapGet("/categories/by-slug/{slug}", GetCategoryBySlugAsync)
            .WithName("GetCategoryBySlug");

        group.MapGet("/brands", ListBrandsAsync)
            .WithName("ListBrands");

        group.MapGet("/brands/by-slug/{slug}", GetBrandBySlugAsync)
            .WithName("GetBrandBySlug");

        return app;
    }

    // -------- contracts (kept in-file because they are storefront-only projections) --------
    public record ProductListItem(
        long Id, string Name, string Slug, decimal Price, decimal? OldPrice,
        string? ThumbnailUrl, bool IsCallForPricing, bool IsAllowToOrder,
        double? RatingAverage, int ReviewsCount);

    public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

    public record CategoryItem(long Id, string Name, string Slug, long? ParentId, int DisplayOrder);

    public record BrandItem(long Id, string Name, string Slug);

    public record ProductDetailResponse(
        long Id, string Name, string Slug, string? Description, string? Specification,
        decimal Price, decimal? OldPrice, int StockQuantity, bool IsCallForPricing,
        bool IsAllowToOrder, string? ThumbnailUrl, IReadOnlyList<string> Images,
        IReadOnlyList<CategoryItem> Categories, BrandItem? Brand,
        double? RatingAverage, int ReviewsCount);

    // -------- handlers --------
    private static async Task<Ok<PagedResult<ProductListItem>>> ListProductsAsync(
        IRepository<Product> repository,
        IProductPricingService pricing,
        IMediaService media,
        long? categoryId,
        long? brandId,
        string? search,
        int page = 1,
        int pageSize = 24)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = repository.Query()
            .Include(p => p.ThumbnailImage)
            .Where(p => p.IsPublished && p.IsVisibleIndividually);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.Categories.Any(c => c.CategoryId == categoryId));
        }
        if (brandId.HasValue)
        {
            query = query.Where(p => p.BrandId == brandId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Name, pattern));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(p => p.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rows.Select(p =>
        {
            var calc = pricing.CalculateProductPrice(p);
            return new ProductListItem(
                p.Id, p.Name, p.Slug, calc.Price, calc.OldPrice,
                media.GetThumbnailUrl(p.ThumbnailImage),
                p.IsCallForPricing, p.IsAllowToOrder,
                p.RatingAverage, p.ReviewsCount);
        }).ToList();

        return TypedResults.Ok(new PagedResult<ProductListItem>(items, total, page, pageSize));
    }

    private static async Task<Results<Ok<ProductDetailResponse>, NotFound>> GetProductByIdAsync(
        long id,
        IRepository<Product> repository,
        IProductPricingService pricing,
        IMediaService media)
    {
        var product = await LoadPublishedProduct(repository, p => p.Id == id);
        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(BuildDetail(product, pricing, media));
    }

    private static async Task<Results<Ok<ProductDetailResponse>, NotFound>> GetProductBySlugAsync(
        string slug,
        IRepository<Product> repository,
        IProductPricingService pricing,
        IMediaService media)
    {
        var product = await LoadPublishedProduct(repository, p => p.Slug == slug);
        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(BuildDetail(product, pricing, media));
    }

    private static async Task<Ok<IReadOnlyList<CategoryItem>>> ListCategoriesAsync(
        IRepository<Category> repository)
    {
        var rows = await repository.Query()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryItem(c.Id, c.Name, c.Slug, c.ParentId, c.DisplayOrder))
            .ToListAsync();
        return TypedResults.Ok((IReadOnlyList<CategoryItem>)rows);
    }

    private static async Task<Results<Ok<CategoryItem>, NotFound>> GetCategoryBySlugAsync(
        string slug,
        IRepository<Category> repository)
    {
        var category = await repository.Query()
            .Where(c => !c.IsDeleted && c.Slug == slug)
            .Select(c => new CategoryItem(c.Id, c.Name, c.Slug, c.ParentId, c.DisplayOrder))
            .FirstOrDefaultAsync();
        return category is null ? TypedResults.NotFound() : TypedResults.Ok(category);
    }

    private static async Task<Ok<IReadOnlyList<BrandItem>>> ListBrandsAsync(
        IRepository<Brand> repository)
    {
        var rows = await repository.Query()
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => new BrandItem(b.Id, b.Name, b.Slug))
            .ToListAsync();
        return TypedResults.Ok((IReadOnlyList<BrandItem>)rows);
    }

    private static async Task<Results<Ok<BrandItem>, NotFound>> GetBrandBySlugAsync(
        string slug,
        IRepository<Brand> repository)
    {
        var brand = await repository.Query()
            .Where(b => !b.IsDeleted && b.Slug == slug)
            .Select(b => new BrandItem(b.Id, b.Name, b.Slug))
            .FirstOrDefaultAsync();
        return brand is null ? TypedResults.NotFound() : TypedResults.Ok(brand);
    }

    // -------- helpers --------
    private static async Task<Product?> LoadPublishedProduct(
        IRepository<Product> repository,
        Expression<Func<Product, bool>> predicate)
    {
        return await repository.Query()
            .Include(p => p.ThumbnailImage)
            .Include(p => p.Brand)
            .Include(p => p.Categories).ThenInclude(c => c.Category)
            .Include(p => p.Medias).ThenInclude(m => m.Media)
            .Where(p => p.IsPublished)
            .FirstOrDefaultAsync(predicate);
    }

    private static ProductDetailResponse BuildDetail(Product product, IProductPricingService pricing, IMediaService media)
    {
        var calc = pricing.CalculateProductPrice(product);
        var categories = product.Categories
            .Select(c => new CategoryItem(c.CategoryId, c.Category.Name, c.Category.Slug,
                c.Category.ParentId, c.Category.DisplayOrder))
            .ToList();
        var brand = product.Brand is null ? null : new BrandItem(product.Brand.Id, product.Brand.Name, product.Brand.Slug);
        var images = product.Medias
            .Where(m => m.Media.MediaType == SimplCommerce.Module.Core.Models.MediaType.Image)
            .Select(m => media.GetMediaUrl(m.Media))
            .ToList();

        return new ProductDetailResponse(
            product.Id, product.Name, product.Slug,
            product.Description, product.Specification,
            calc.Price, calc.OldPrice, product.StockQuantity,
            product.IsCallForPricing, product.IsAllowToOrder,
            media.GetThumbnailUrl(product.ThumbnailImage),
            images, categories, brand,
            product.RatingAverage, product.ReviewsCount);
    }
}

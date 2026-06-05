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
using SimplCommerce.Module.Cms.Models;

namespace SimplCommerce.Module.Cms.Endpoints;

public static class CmsStorefrontEndpoints
{
    public record PageItem(long Id, string Name, string Slug, string? Body);
    public record MenuItemDto(long Id, string Name, string? CustomLink, int DisplayOrder);

    public static IEndpointRouteBuilder MapCmsStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/cms").WithTags("Storefront.Cms");

        group.MapGet("/pages/{slug}", async (string slug, IRepository<Page> pages) =>
        {
            var page = await pages.Query()
                .Where(p => !p.IsDeleted && p.IsPublished && p.Slug == slug)
                .Select(p => new PageItem(p.Id, p.Name, p.Slug, p.Body))
                .FirstOrDefaultAsync();
            return page is null ? Results.NotFound() : Results.Ok(page);
        });

        group.MapGet("/menus/{name}", async (string name, IRepository<Menu> menus) =>
        {
            var menu = await menus.Query()
                .Include(m => m.MenuItems)
                .Where(m => m.IsPublished && m.Name == name)
                .FirstOrDefaultAsync();
            if (menu is null) return Results.NotFound();
            var items = menu.MenuItems
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new MenuItemDto(i.Id, i.Name, i.CustomLink, i.DisplayOrder))
                .ToList();
            return Results.Ok(items);
        });

        return app;
    }
}

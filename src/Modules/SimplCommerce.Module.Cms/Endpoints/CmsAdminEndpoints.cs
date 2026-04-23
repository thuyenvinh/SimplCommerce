#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Cms.Models;

namespace SimplCommerce.Module.Cms.Endpoints;

public static class CmsAdminEndpoints
{
    public record PageInput(string Name, string Slug, string? Body, bool IsPublished);

    public static IEndpointRouteBuilder MapCmsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/cms")
            .WithTags("Admin.Cms")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/pages", async (IRepository<Page> repo) =>
        {
            var list = await repo.Query().Where(p => !p.IsDeleted)
                .Select(p => new { p.Id, p.Name, p.Slug, p.IsPublished, p.CreatedOn }).ToListAsync();
            return Results.Ok(list);
        });

        group.MapPost("/pages", async (PageInput input, IRepository<Page> repo) =>
        {
            var page = new Page { Name = input.Name, Slug = input.Slug, Body = input.Body ?? string.Empty, IsPublished = input.IsPublished };
            repo.Add(page);
            repo.SaveChanges();
            return Results.Created($"/api/admin/cms/pages/{page.Id}", new { page.Id });
        });

        group.MapPut("/pages/{id:long}", async (long id, PageInput input, IRepository<Page> repo) =>
        {
            var page = await repo.Query().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (page is null) return Results.NotFound();
            page.Name = input.Name;
            page.Slug = input.Slug;
            page.Body = input.Body ?? string.Empty;
            page.IsPublished = input.IsPublished;
            repo.SaveChanges();
            return Results.NoContent();
        });

        group.MapDelete("/pages/{id:long}", async (long id, IRepository<Page> repo) =>
        {
            var page = await repo.Query().FirstOrDefaultAsync(p => p.Id == id);
            if (page is null) return Results.NotFound();
            page.IsDeleted = true;
            repo.SaveChanges();
            return Results.NoContent();
        });

        return app;
    }
}

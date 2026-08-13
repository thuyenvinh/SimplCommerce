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
    public record PageDetail(long Id, string Name, string Slug, string? Body, bool IsPublished, System.DateTimeOffset CreatedOn);
    public record PageListItem(long Id, string Name, string Slug, bool IsPublished, System.DateTimeOffset CreatedOn);

    // Wave 17: editable email templates
    public record EmailTemplateInput(string Key, string Name, string Subject, string? BodyHtml, bool IsActive);
    public record EmailTemplateItem(long Id, string Key, string Name, string Subject, string? BodyHtml, bool IsActive, System.DateTimeOffset LatestUpdatedOn);

    public static IEndpointRouteBuilder MapCmsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/cms")
            .WithTags("Admin.Cms")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/pages", async (IRepository<Page> repo) =>
        {
            var list = await repo.Query().Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedOn)
                .Select(p => new PageListItem(p.Id, p.Name, p.Slug, p.IsPublished, p.CreatedOn)).ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/pages/{id:long}", async (long id, IRepository<Page> repo) =>
        {
            var page = await repo.Query().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            return page is null
                ? Results.NotFound()
                : Results.Ok(new PageDetail(page.Id, page.Name, page.Slug, page.Body, page.IsPublished, page.CreatedOn));
        });

        group.MapPost("/pages", (PageInput input, IRepository<Page> repo) =>
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

        // ---- Email templates (Wave 17) ----
        group.MapGet("/email-templates", async (IRepository<EmailTemplate> repo) =>
        {
            var list = await repo.Query()
                .OrderBy(t => t.Key)
                .Select(t => new EmailTemplateItem(t.Id, t.Key, t.Name, t.Subject, t.BodyHtml, t.IsActive, t.LatestUpdatedOn))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/email-templates/{id:long}", async (long id, IRepository<EmailTemplate> repo) =>
        {
            var t = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            return t is null
                ? Results.NotFound()
                : Results.Ok(new EmailTemplateItem(t.Id, t.Key, t.Name, t.Subject, t.BodyHtml, t.IsActive, t.LatestUpdatedOn));
        });

        group.MapPost("/email-templates", async (EmailTemplateInput input, IRepository<EmailTemplate> repo) =>
        {
            if (string.IsNullOrWhiteSpace(input.Key) || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Subject))
            {
                return Results.BadRequest(new { error = "Key, Name and Subject are required." });
            }
            if (await repo.Query().AnyAsync(t => t.Key == input.Key))
            {
                return Results.Conflict(new { error = "Template key already exists.", field = "Key" });
            }
            var entity = new EmailTemplate
            {
                Key = input.Key,
                Name = input.Name,
                Subject = input.Subject,
                BodyHtml = input.BodyHtml ?? string.Empty,
                IsActive = input.IsActive,
            };
            repo.Add(entity);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/cms/email-templates/{entity.Id}", new { entity.Id });
        });

        group.MapPut("/email-templates/{id:long}", async (long id, EmailTemplateInput input, IRepository<EmailTemplate> repo) =>
        {
            var entity = await repo.Query().FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null) return Results.NotFound();
            // Key is not editable post-create — services look it up by Key and a
            // rename would silently break callers. Admin can deactivate + create
            // a new template if they need a different key.
            entity.Name = input.Name;
            entity.Subject = input.Subject;
            entity.BodyHtml = input.BodyHtml ?? string.Empty;
            entity.IsActive = input.IsActive;
            entity.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/email-templates/{id:long}", async (long id, IRepository<EmailTemplate> repo) =>
        {
            var entity = await repo.Query().FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null) return Results.NotFound();
            repo.Remove(entity);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Contacts.Models;

namespace SimplCommerce.Module.Contacts.Endpoints;

/// <summary>
/// Admin inbox for the public contact form. The Contact entity has no first-class
/// "handled / replied" status, so this exposes:
///   • list (filterable by status, where status == IsDeleted projected to 0/1)
///   • detail
///   • mark-handled (PATCH status -> 1, soft-deletes the row)
///
/// A richer status workflow (Open / InProgress / Replied) would require a schema
/// change — left as a follow-up.
/// </summary>
public static class ContactsAdminEndpoints
{
    public record AdminContactItem(long Id, string Name, string Email, string Message, int Status, long? ContactAreaId, System.DateTimeOffset CreatedOn);
    public record AdminContactsPage(int Total, int Page, int PageSize, System.Collections.Generic.IReadOnlyList<AdminContactItem> Items);
    public record AdminContactStatusInput(int Status);

    public static IEndpointRouteBuilder MapContactsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/contacts")
            .WithTags("Admin.Contacts")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IRepository<Contact> repo, int page = 1, int pageSize = 20, int? status = null) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var query = repo.Query();
            if (status.HasValue)
            {
                var deleted = status.Value == 1;
                query = query.Where(c => c.IsDeleted == deleted);
            }
            var total = await query.CountAsync();
            var rows = await query
                .OrderByDescending(c => c.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new AdminContactItem(
                    c.Id, c.FullName ?? string.Empty, c.EmailAddress ?? string.Empty,
                    c.Content ?? string.Empty, c.IsDeleted ? 1 : 0,
                    c.ContactAreaId, c.CreatedOn))
                .ToListAsync();
            return Results.Ok(new AdminContactsPage(total, page, pageSize, rows));
        });

        group.MapGet("/{id:long}", async (long id, IRepository<Contact> repo) =>
        {
            var c = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return Results.NotFound();
            return Results.Ok(new AdminContactItem(
                c.Id, c.FullName ?? string.Empty, c.EmailAddress ?? string.Empty,
                c.Content ?? string.Empty, c.IsDeleted ? 1 : 0,
                c.ContactAreaId, c.CreatedOn));
        });

        group.MapPatch("/{id:long}/status", async (long id, AdminContactStatusInput input, IRepository<Contact> repo) =>
        {
            var c = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return Results.NotFound();
            c.IsDeleted = input.Status == 1;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

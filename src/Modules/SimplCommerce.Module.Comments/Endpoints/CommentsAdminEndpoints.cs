#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Comments.Models;

namespace SimplCommerce.Module.Comments.Endpoints;

/// <summary>
/// Admin moderation API. Storefront comments POST flow stays as it is (legacy),
/// admin gets a paginated queue + status PATCH + soft delete.
/// </summary>
public static class CommentsAdminEndpoints
{
    public record AdminCommentItem(long Id, string CommenterName, string CommenterEmail, string CommentText, int Status, System.DateTimeOffset CreatedOn, string EntityTypeId, long EntityId);
    public record AdminCommentsPage(int Total, int Page, int PageSize, System.Collections.Generic.IReadOnlyList<AdminCommentItem> Items);
    public record AdminCommentStatusInput(int Status);

    public static IEndpointRouteBuilder MapCommentsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/comments")
            .WithTags("Admin.Comments")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IRepository<Comment> repo, int page = 1, int pageSize = 20, int? status = null) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var query = repo.Query();
            if (status.HasValue)
            {
                var asEnum = (CommentStatus)status.Value;
                query = query.Where(c => c.Status == asEnum);
            }
            var total = await query.CountAsync();
            var rows = await query
                .OrderByDescending(c => c.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new AdminCommentItem(
                    c.Id, c.CommenterName ?? string.Empty, c.User != null ? c.User.Email ?? string.Empty : string.Empty,
                    c.CommentText ?? string.Empty, (int)c.Status, c.CreatedOn,
                    c.EntityTypeId ?? string.Empty, c.EntityId))
                .ToListAsync();
            return Results.Ok(new AdminCommentsPage(total, page, pageSize, rows));
        });

        group.MapPatch("/{id:long}/status", async (long id, AdminCommentStatusInput input, IRepository<Comment> repo) =>
        {
            var comment = await repo.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (comment is null) return Results.NotFound();
            comment.Status = (CommentStatus)input.Status;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:long}", async (long id, IRepository<Comment> repo) =>
        {
            var comment = await repo.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (comment is null) return Results.NotFound();
            repo.Remove(comment);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

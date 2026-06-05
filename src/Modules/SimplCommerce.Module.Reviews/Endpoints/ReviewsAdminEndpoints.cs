#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Reviews.Models;

namespace SimplCommerce.Module.Reviews.Endpoints;

public static class ReviewsAdminEndpoints
{
    public record ModerationRequest(ReviewStatus Status);

    public static IEndpointRouteBuilder MapReviewsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/reviews")
            .WithTags("Admin.Reviews")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IRepository<Review> repo, ReviewStatus? status = null, int page = 1, int pageSize = 20) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var query = repo.Query().AsQueryable();
            if (status.HasValue) query = query.Where(r => r.Status == status);
            var total = await query.CountAsync();
            var rows = await query.OrderByDescending(r => r.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new { r.Id, r.Rating, r.Title, r.Comment, r.ReviewerName, r.Status, r.CreatedOn, r.EntityTypeId, r.EntityId })
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        group.MapPatch("/{id:long}/status", async (long id, ModerationRequest req, IRepository<Review> repo) =>
        {
            var review = await repo.Query().FirstOrDefaultAsync(r => r.Id == id);
            if (review is null) return Results.NotFound();
            review.Status = req.Status;
            repo.SaveChanges();
            return Results.NoContent();
        });

        return app;
    }
}

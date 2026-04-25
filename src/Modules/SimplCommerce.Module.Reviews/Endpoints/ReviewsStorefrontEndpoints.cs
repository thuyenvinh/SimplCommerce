#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Reviews.Models;

namespace SimplCommerce.Module.Reviews.Endpoints;

public static class ReviewsStorefrontEndpoints
{
    public record ReviewItemDto(long Id, int Rating, string Title, string Comment, string ReviewerName, DateTimeOffset CreatedOn);
    public record SubmitReviewRequest(string EntityTypeId, long EntityId, int Rating, string Title, string Comment);

    public static IEndpointRouteBuilder MapReviewsStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/reviews").WithTags("Storefront.Reviews");

        group.MapGet("/", async (
            string entityTypeId,
            long entityId,
            IRepository<Review> repo,
            int page = 1,
            int pageSize = 10) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var query = repo.Query()
                .Where(r => r.EntityTypeId == entityTypeId && r.EntityId == entityId && r.Status == ReviewStatus.Approved)
                .OrderByDescending(r => r.CreatedOn);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new ReviewItemDto(r.Id, r.Rating, r.Title, r.Comment, r.ReviewerName, r.CreatedOn))
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items });
        });

        group.MapPost("/", (SubmitReviewRequest req, IRepository<Review> repo, ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            if (req.Rating < 1 || req.Rating > 5) return Results.BadRequest("Rating must be between 1 and 5.");
            var review = new Review
            {
                UserId = userId,
                EntityTypeId = req.EntityTypeId,
                EntityId = req.EntityId,
                Rating = req.Rating,
                Title = req.Title,
                Comment = req.Comment,
                ReviewerName = principal.FindFirstValue(ClaimTypes.Name) ?? "Anonymous",
                Status = ReviewStatus.Pending,
                CreatedOn = DateTimeOffset.UtcNow,
            };
            repo.Add(review);
            repo.SaveChanges();
            return Results.Created($"/api/storefront/reviews/{review.Id}", new ReviewItemDto(
                review.Id, review.Rating, review.Title, review.Comment, review.ReviewerName, review.CreatedOn));
        }).RequireAuthorization("CustomerOnly");

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId)
    {
        userId = 0;
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return long.TryParse(raw, out userId);
    }
}

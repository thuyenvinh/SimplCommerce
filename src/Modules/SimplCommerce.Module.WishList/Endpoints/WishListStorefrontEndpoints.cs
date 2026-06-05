#nullable enable
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
using SimplCommerce.Module.WishList.Models;
using WishListEntity = SimplCommerce.Module.WishList.Models.WishList;

namespace SimplCommerce.Module.WishList.Endpoints;

public static class WishListStorefrontEndpoints
{
    public record WishListItemDto(long Id, long ProductId, int Quantity, string? Description);
    public record AddWishListItemRequest(long ProductId, int Quantity = 1, string? Description = null);

    public static IEndpointRouteBuilder MapWishListStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/wishlist")
            .WithTags("Storefront.WishList")
            .RequireAuthorization("CustomerOnly");

        group.MapGet("/", async (IRepository<WishListItem> repo, ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var items = await repo.Query()
                .Where(i => i.WishList.UserId == userId)
                .Select(i => new WishListItemDto(i.Id, i.ProductId, i.Quantity, i.Description))
                .ToListAsync();
            return Results.Ok(items);
        });

        group.MapPost("/items", async (AddWishListItemRequest req, IRepository<WishListEntity> wishLists, IRepository<WishListItem> items, ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();

            var wishList = await wishLists.Query().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wishList is null)
            {
                wishList = new WishListEntity { UserId = userId, CreatedOn = System.DateTimeOffset.UtcNow };
                wishLists.Add(wishList);
                wishLists.SaveChanges();
            }

            var item = new WishListItem
            {
                WishListId = wishList.Id,
                ProductId = req.ProductId,
                Quantity = req.Quantity,
                Description = req.Description ?? string.Empty,
                CreatedOn = System.DateTimeOffset.UtcNow,
                LatestUpdatedOn = System.DateTimeOffset.UtcNow
            };
            items.Add(item);
            items.SaveChanges();
            return Results.Created($"/api/storefront/wishlist/items/{item.Id}", new WishListItemDto(item.Id, item.ProductId, item.Quantity, item.Description));
        });

        group.MapDelete("/items/{id:long}", async (long id, IRepository<WishListItem> repo, ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var item = await repo.Query().FirstOrDefaultAsync(i => i.Id == id && i.WishList.UserId == userId);
            if (item is null) return Results.NotFound();
            repo.Remove(item);
            repo.SaveChanges();
            return Results.NoContent();
        });

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId)
    {
        userId = 0;
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return long.TryParse(raw, out userId);
    }
}

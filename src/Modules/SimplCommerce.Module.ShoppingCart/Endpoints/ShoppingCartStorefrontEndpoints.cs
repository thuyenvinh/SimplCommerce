#nullable enable
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.ShoppingCart.Endpoints;

/// <summary>
/// Storefront cart API. Customer auth required — guest carts are owned by the Storefront
/// BFF (Phase 4) which maps them to a synthetic user before proxying to this endpoint.
/// </summary>
public static class ShoppingCartStorefrontEndpoints
{
    public record AddToCartRequest(long ProductId, int Quantity);
    public record UpdateCartItemRequest(long ProductId, int Quantity);
    public record ApplyCouponRequest(string CouponCode);

    public static IEndpointRouteBuilder MapShoppingCartStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/cart")
            .WithTags("Storefront.Cart")
            .RequireAuthorization("CustomerOnly");

        group.MapGet("/", GetCartAsync);
        group.MapPost("/items", AddToCartAsync);
        group.MapPut("/items", UpdateCartItemAsync);
        group.MapPost("/coupon", ApplyCouponAsync);

        return app;
    }

    private static async Task<IResult> GetCartAsync(ICartService cartService, ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId)) return TypedResults.Unauthorized();
        return TypedResults.Ok(await cartService.GetCartDetails(userId));
    }

    private static async Task<IResult> AddToCartAsync(AddToCartRequest req, ICartService cartService, ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId)) return TypedResults.Unauthorized();
        var result = await cartService.AddToCart(userId, req.ProductId, req.Quantity);
        return result.ErrorMessage is null ? TypedResults.Ok(result) : TypedResults.BadRequest(result.ErrorMessage);
    }

    private static async Task<IResult> UpdateCartItemAsync(UpdateCartItemRequest req, ICartService cartService, ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId)) return TypedResults.Unauthorized();
        // AddToCart is idempotent-ish: it overwrites the quantity, so it doubles as an update.
        var result = await cartService.AddToCart(userId, req.ProductId, req.Quantity);
        return result.ErrorMessage is null ? TypedResults.Ok(result) : TypedResults.BadRequest(result.ErrorMessage);
    }

    private static async Task<IResult> ApplyCouponAsync(ApplyCouponRequest req, ICartService cartService, ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId)) return TypedResults.Unauthorized();
        var applied = await cartService.ApplyCoupon(userId, req.CouponCode);
        return applied.Succeeded ? TypedResults.Ok(applied) : TypedResults.BadRequest(applied);
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId)
    {
        userId = 0;
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return long.TryParse(raw, out userId);
    }
}

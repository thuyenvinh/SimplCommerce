#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using SimplCommerce.Module.Checkouts.Models;
using SimplCommerce.Module.Checkouts.Services;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;
using SimplCommerce.Module.Payments.Models;
using SimplCommerce.Module.ShoppingCart.Models;

namespace SimplCommerce.ApiService.CheckoutFlow;

/// <summary>
/// Storefront checkout flow. Replaces the legacy MVC <c>CheckoutController</c> with
/// JSON endpoints that the Blazor Storefront drives.
///
/// Stages:
/// 1. <c>POST   /api/storefront/checkout</c>               — create Checkout from current cart
/// 2. <c>GET    /api/storefront/checkout/{id}</c>          — checkout summary (totals, items)
/// 3. <c>POST   /api/storefront/checkout/{id}/shipping</c> — save address + recalculate tax/shipping
/// 4. <c>GET    /api/storefront/checkout/payment-methods</c> — enabled payment providers
/// 5. <c>POST   /api/storefront/checkout/{id}/place-order</c> — finalize via IOrderService.CreateOrder
///
/// Ownership is checked on every mutate via <c>checkout.CreatedById == currentUserId</c>.
/// The shipping payload is serialized into the same <see cref="DeliveryInformationVm"/>
/// JSON shape the legacy stack uses, so <c>OrderService.CreateOrder</c> reads it
/// unchanged — no domain refactor needed.
/// </summary>
public static class CheckoutStorefrontEndpoints
{
    public record CheckoutAddressRequest(
        string ContactName,
        string Phone,
        string AddressLine1,
        string? AddressLine2,
        string CountryId,
        long StateOrProvinceId,
        long? DistrictId,
        string? City,
        string? ZipCode,
        bool UseShippingAddressAsBillingAddress,
        string? ShippingMethod,
        string? OrderNote);

    public record CheckoutSummary(
        Guid Id,
        decimal SubTotal,
        decimal Discount,
        decimal? ShippingAmount,
        decimal? TaxAmount,
        decimal OrderTotal,
        string? CouponCode,
        IReadOnlyList<CheckoutLine> Items);

    public record CheckoutLine(long ProductId, string ProductName, int Quantity, decimal ProductPrice);

    public record PaymentMethodOption(string Id, string Name);

    public record PlaceOrderRequest(string PaymentMethod, decimal PaymentFeeAmount);

    public record PlaceOrderResponse(long OrderId);

    public static IEndpointRouteBuilder MapCheckoutStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/checkout")
            .WithTags("Storefront.Checkout")
            .RequireAuthorization("CustomerOnly");

        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/shipping", SaveShippingAsync);
        group.MapGet("/payment-methods", ListPaymentMethodsAsync).AllowAnonymous();
        group.MapPost("/{id:guid}/place-order", PlaceOrderAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        ICheckoutService checkoutService,
        IRepository<CartItem> cartRepo,
        ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();

        var cartItems = await cartRepo.Query()
            .Where(x => x.CustomerId == userId)
            .Select(x => new CartItemToCheckoutVm { ProductId = x.ProductId, Quantity = x.Quantity })
            .ToListAsync();

        if (cartItems.Count == 0)
        {
            return Results.BadRequest(new { error = "Cart is empty." });
        }

        var checkout = await checkoutService.Create(userId, userId, cartItems, couponCode: null!);
        return Results.Ok(new { checkoutId = checkout.Id });
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ICheckoutService checkoutService,
        IRepositoryWithTypedId<Checkout, Guid> checkoutRepo,
        ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        if (!await OwnsCheckoutAsync(checkoutRepo, id, userId)) return Results.NotFound();

        var vm = await checkoutService.GetCheckoutDetails(id);
        if (vm is null) return Results.NotFound();

        var lines = vm.Items.Select(i => new CheckoutLine(
            i.ProductId, i.ProductName, i.Quantity, i.ProductPrice)).ToList();

        return Results.Ok(new CheckoutSummary(
            vm.Id, vm.SubTotal, vm.Discount, vm.ShippingAmount, vm.TaxAmount, vm.OrderTotal,
            vm.CouponCode, lines));
    }

    private static async Task<IResult> SaveShippingAsync(
        Guid id,
        CheckoutAddressRequest req,
        IRepositoryWithTypedId<Checkout, Guid> checkoutRepo,
        ICheckoutService checkoutService,
        IRepositoryWithTypedId<Country, string> countryRepo,
        IRepository<StateOrProvince> stateRepo,
        IRepository<District> districtRepo,
        ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        var checkout = await checkoutRepo.Query().FirstOrDefaultAsync(x => x.Id == id);
        if (checkout is null || checkout.CreatedById != userId) return Results.NotFound();

        // G12: validate the geo selection BEFORE persisting shipping data. Without these
        // checks an arbitrary CountryId / StateOrProvinceId pair sneaks through, then
        // OrderService.CreateOrder later joins on the missing ids and either returns
        // unhelpful 500s or silently writes addresses against deleted/non-existent rows.
        // The three-way check enforces both existence + parent-child consistency:
        //   • Country must exist + be shipping-enabled
        //   • StateOrProvince must exist AND belong to the chosen country
        //   • District (when provided) must belong to the chosen state
        var country = await countryRepo.Query().FirstOrDefaultAsync(c => c.Id == req.CountryId);
        if (country is null || !country.IsShippingEnabled)
        {
            return Results.BadRequest(new { error = "Country is not available for shipping.", field = nameof(req.CountryId) });
        }
        var state = await stateRepo.Query().FirstOrDefaultAsync(s => s.Id == req.StateOrProvinceId);
        if (state is null || state.CountryId != country.Id)
        {
            return Results.BadRequest(new { error = "State/province doesn't belong to the selected country.", field = nameof(req.StateOrProvinceId) });
        }
        if (req.DistrictId is { } districtId)
        {
            var districtBelongs = await districtRepo.Query()
                .AnyAsync(d => d.Id == districtId && d.StateOrProvinceId == state.Id);
            if (!districtBelongs)
            {
                return Results.BadRequest(new { error = "District doesn't belong to the selected state.", field = nameof(req.DistrictId) });
            }
        }

        var delivery = new DeliveryInformationVm
        {
            CheckoutId = id,
            ShippingAddressId = 0,
            BillingAddressId = 0,
            UseShippingAddressAsBillingAddress = req.UseShippingAddressAsBillingAddress,
            ShippingMethod = req.ShippingMethod,
            OrderNote = req.OrderNote,
            NewAddressForm = new AddressFormVm
            {
                ContactName = req.ContactName,
                Phone = req.Phone,
                AddressLine1 = req.AddressLine1,
                AddressLine2 = req.AddressLine2,
                CountryId = req.CountryId,
                StateOrProvinceId = req.StateOrProvinceId,
                DistrictId = req.DistrictId,
                City = req.City,
                ZipCode = req.ZipCode,
            },
            NewBillingAddressForm = new AddressFormVm
            {
                ContactName = req.ContactName,
                Phone = req.Phone,
                AddressLine1 = req.AddressLine1,
                AddressLine2 = req.AddressLine2,
                CountryId = req.CountryId,
                StateOrProvinceId = req.StateOrProvinceId,
                DistrictId = req.DistrictId,
                City = req.City,
                ZipCode = req.ZipCode,
            },
        };
        checkout.ShippingData = JsonConvert.SerializeObject(delivery);
        await checkoutRepo.SaveChangesAsync();

        var taxAndShipping = await checkoutService.UpdateTaxAndShippingPrices(id, new TaxAndShippingPriceRequestVm
        {
            NewShippingAddress = new ShippingAddressVm
            {
                CountryName = req.CountryId,
                StateOrProvinceName = req.StateOrProvinceId.ToString(),
                ZipCode = req.ZipCode,
            },
            NewBillingAddress = new ShippingAddressVm
            {
                CountryName = req.CountryId,
                StateOrProvinceName = req.StateOrProvinceId.ToString(),
                ZipCode = req.ZipCode,
            },
            SelectedShippingMethodName = req.ShippingMethod ?? string.Empty,
        });

        return Results.Ok(new
        {
            taxAmount = taxAndShipping.CheckoutVm?.TaxAmount,
            shippingAmount = taxAndShipping.CheckoutVm?.ShippingAmount,
            orderTotal = taxAndShipping.CheckoutVm?.OrderTotal,
            shippingOptions = taxAndShipping.ShippingPrices is null
                ? Enumerable.Empty<object>()
                : taxAndShipping.ShippingPrices.Select(s => (object)new { s.Name, s.Price }),
        });
    }

    private static async Task<IResult> ListPaymentMethodsAsync(
        IRepositoryWithTypedId<PaymentProvider, string> providers)
    {
        var enabled = await providers.Query()
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Name)
            .Select(p => new PaymentMethodOption(p.Id, p.Name))
            .ToListAsync();

        return Results.Ok(enabled);
    }

    private static async Task<IResult> PlaceOrderAsync(
        Guid id,
        PlaceOrderRequest req,
        IOrderService orderService,
        IRepositoryWithTypedId<Checkout, Guid> checkoutRepo,
        ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        var checkout = await checkoutRepo.Query().FirstOrDefaultAsync(x => x.Id == id);
        if (checkout is null || checkout.CreatedById != userId) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(checkout.ShippingData))
        {
            return Results.BadRequest(new { error = "Shipping address is required before placing an order." });
        }

        var result = await orderService.CreateOrder(
            id,
            req.PaymentMethod,
            Math.Max(0m, req.PaymentFeeAmount),
            OrderStatus.PendingPayment);

        if (!result.Success)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(new PlaceOrderResponse(result.Value.Id));
    }

    private static async Task<bool> OwnsCheckoutAsync(IRepositoryWithTypedId<Checkout, Guid> repo, Guid checkoutId, long userId)
        => await repo.Query().AnyAsync(c => c.Id == checkoutId && c.CreatedById == userId);

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId)
    {
        userId = 0;
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return long.TryParse(raw, out userId);
    }
}

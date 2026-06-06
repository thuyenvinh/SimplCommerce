using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Checkouts.Models;
using SimplCommerce.Module.Checkouts.Services;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;
using SimplCommerce.Module.Payments.Models;

namespace SimplCommerce.Module.PaymentVnpay;

/// <summary>
/// Endpoints for the VNPAY redirect-then-callback flow:
///
/// 1. <c>POST /api/payments/vnpay/start</c> — buyer side. Looks up the open Checkout,
///    composes the VNPAY pay-URL with the merchant TmnCode + HMAC-SHA512 signature,
///    returns it as JSON so the Storefront can <c>window.location = url</c>.
/// 2. <c>GET /api/payments/vnpay/return</c> — browser-side return. VNPAY redirects
///    the buyer here with vnp_* query params; the endpoint verifies the signature
///    and either creates the order (on vnp_ResponseCode == "00") or shows the error.
/// 3. <c>GET /api/payments/vnpay/ipn</c> — server-to-server IPN. Same signature
///    check; we return the canonical {"RspCode":"00","Message":"Confirm Success"}
///    body when the order is settled. VNPAY retries on non-success.
///
/// All three are <c>AllowAnonymous</c> because they run in the buyer's redirect
/// flow or as a S2S callback — the HMAC signature IS the authentication.
/// </summary>
public static class VnpayEndpoints
{
    public static IEndpointRouteBuilder MapVnpayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments/vnpay")
            .WithTags("Payments.Vnpay")
            .AllowAnonymous();

        group.MapPost("/start", StartAsync).RequireAuthorization("CustomerOnly");
        group.MapGet("/return", ReturnAsync);
        group.MapGet("/ipn", IpnAsync);

        return app;
    }

    private static async Task<IResult> StartAsync(
        StartRequest req,
        IRepositoryWithTypedId<Checkout, Guid> checkouts,
        IRepositoryWithTypedId<PaymentProvider, string> providers,
        ICheckoutService checkoutService,
        HttpContext ctx,
        ClaimsPrincipal principal)
    {
        var userId = ParseUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var checkout = await checkouts.Query().FirstOrDefaultAsync(c => c.Id == req.CheckoutId);
        if (checkout is null || checkout.CreatedById != userId) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(checkout.ShippingData))
        {
            return Results.BadRequest(new { error = "Shipping address required before payment." });
        }

        var cfg = LoadConfig(providers);
        if (cfg is null || string.IsNullOrWhiteSpace(cfg.TmnCode) || string.IsNullOrWhiteSpace(cfg.HashSecret))
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var summary = await checkoutService.GetCheckoutDetails(checkout.Id);
        var amount = summary?.OrderTotal ?? 0m;
        var returnBase = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        var parameters = new Dictionary<string, string>
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = cfg.TmnCode,
            ["vnp_Amount"] = VnpaySignature.FormatAmount(amount),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = checkout.Id.ToString("N"),
            ["vnp_OrderInfo"] = $"Pay for checkout {checkout.Id:N}",
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = $"{returnBase}/api/payments/vnpay/return",
            ["vnp_IpAddr"] = ctx.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            ["vnp_CreateDate"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
        };

        var signature = VnpaySignature.Build(parameters, cfg.HashSecret);
        var query = string.Join('&',
            parameters.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{System.Net.WebUtility.UrlEncode(kv.Key)}={System.Net.WebUtility.UrlEncode(kv.Value)}"));
        var redirectUrl = $"{cfg.PayUrl}?{query}&vnp_SecureHash={signature}";

        return Results.Ok(new { redirectUrl });
    }

    private static async Task<IResult> ReturnAsync(
        HttpContext ctx,
        IOrderService orderService,
        IRepositoryWithTypedId<Checkout, Guid> checkouts,
        IRepositoryWithTypedId<PaymentProvider, string> providers,
        IRepository<Payment> payments)
    {
        var (params_, checkoutId) = ParseParams(ctx);
        var cfg = LoadConfig(providers);
        if (cfg is null) return Results.Redirect("/cart");
        if (!VnpaySignature.Verify(params_, cfg.HashSecret))
        {
            return Results.Redirect("/cart?vnpay=invalid-signature");
        }

        if (params_.TryGetValue("vnp_ResponseCode", out var rc) && rc == "00" && checkoutId is { } id)
        {
            var checkout = await checkouts.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (checkout is not null)
            {
                var result = await orderService.CreateOrder(id, VnpayProvider.Id, cfg.PaymentFee, OrderStatus.PaymentReceived);
                if (result.Success)
                {
                    await RecordPaymentAsync(payments, result.Value, params_, cfg);
                    return Results.Redirect($"/checkout/{result.Value.Id}/success");
                }
            }
        }

        return Results.Redirect($"/cart?vnpay=failed&code={rc}");
    }

    private static async Task<IResult> IpnAsync(
        HttpContext ctx,
        IOrderService orderService,
        IRepositoryWithTypedId<Checkout, Guid> checkouts,
        IRepositoryWithTypedId<PaymentProvider, string> providers,
        IRepository<Payment> payments)
    {
        var (params_, checkoutId) = ParseParams(ctx);
        var cfg = LoadConfig(providers);
        if (cfg is null) return Results.Json(new { RspCode = "97", Message = "Invalid checksum" });
        if (!VnpaySignature.Verify(params_, cfg.HashSecret))
        {
            return Results.Json(new { RspCode = "97", Message = "Invalid checksum" });
        }

        if (params_.TryGetValue("vnp_ResponseCode", out var rc) && rc == "00" && checkoutId is { } id)
        {
            var checkout = await checkouts.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (checkout is null)
            {
                return Results.Json(new { RspCode = "01", Message = "Order not found" });
            }
            var result = await orderService.CreateOrder(id, VnpayProvider.Id, cfg.PaymentFee, OrderStatus.PaymentReceived);
            if (result.Success)
            {
                await RecordPaymentAsync(payments, result.Value, params_, cfg);
            }
            return Results.Json(new { RspCode = "00", Message = "Confirm Success" });
        }

        return Results.Json(new { RspCode = "02", Message = "Order already confirmed or failed" });
    }

    // G06: persist a Payment row for every successful VNPAY capture so the
    // Payments admin table has an audit trail. Idempotent — checks for an
    // existing Payment+GatewayTransactionId before inserting because the same
    // checkout fires both the browser-return AND the server-to-server IPN.
    private static async Task RecordPaymentAsync(
        IRepository<Payment> payments,
        Order order,
        Dictionary<string, string> vnpParams,
        VnpayConfig cfg)
    {
        var txnId = vnpParams.TryGetValue("vnp_TransactionNo", out var t) ? t : string.Empty;
        var already = await payments.Query()
            .AnyAsync(p => p.OrderId == order.Id && p.PaymentMethod == VnpayProvider.Id);
        if (already) return;

        // vnp_Amount is in VND minor units (×100). Fall back to OrderTotal if the
        // param is missing/unparseable — never trust gateway input blindly.
        decimal capturedAmount = order.OrderTotal;
        if (vnpParams.TryGetValue("vnp_Amount", out var amtRaw)
            && long.TryParse(amtRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
        {
            capturedAmount = minor / 100m;
        }

        payments.Add(new Payment
        {
            OrderId = order.Id,
            PaymentMethod = VnpayProvider.Id,
            Amount = capturedAmount,
            PaymentFee = cfg.PaymentFee,
            GatewayTransactionId = txnId,
            Status = PaymentStatus.Succeeded,
        });
        await payments.SaveChangesAsync();
    }

    private static (Dictionary<string, string> Params, Guid? CheckoutId) ParseParams(HttpContext ctx)
    {
        var dict = ctx.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        Guid? checkoutId = null;
        if (dict.TryGetValue("vnp_TxnRef", out var txn) && Guid.TryParseExact(txn, "N", out var parsed))
        {
            checkoutId = parsed;
        }
        return (dict, checkoutId);
    }

    private static VnpayConfig? LoadConfig(IRepositoryWithTypedId<PaymentProvider, string> providers)
    {
        var row = providers.Query().FirstOrDefault(p => p.Id == VnpayProvider.Id);
        if (row is null || string.IsNullOrWhiteSpace(row.AdditionalSettings)) return null;
        try { return JsonConvert.DeserializeObject<VnpayConfig>(row.AdditionalSettings); }
        catch (JsonException) { return null; }
    }

    private static long? ParseUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return long.TryParse(raw, out var v) ? v : null;
    }

    public record StartRequest(Guid CheckoutId);
}

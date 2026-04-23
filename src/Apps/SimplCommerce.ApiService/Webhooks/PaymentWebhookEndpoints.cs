using Microsoft.AspNetCore.Http.HttpResults;

namespace SimplCommerce.ApiService.Webhooks;

/// <summary>
/// Payment gateway webhook callbacks. Each provider has its own signing/verification
/// scheme — implementations are intentionally stubs until we have sandbox credentials
/// per provider to exercise end-to-end. Every endpoint:
/// 1. MUST verify the signature / IPN token before touching the domain model.
/// 2. MUST be idempotent (providers retry).
/// 3. MUST NOT require auth — the webhook IS the authentication, via the signature.
///
/// TODO per provider (Phase 3.7 sub-PR):
/// - Stripe: verify Stripe-Signature header using webhook secret (Stripe.Events).
/// - PayPal: IPN verify via round-trip to paypal.com/cgi-bin/webscr.
/// - MoMo: verify HMAC SHA256 using SecretKey + sorted query params.
/// - VNPay: module doesn't exist in codebase today; skip until PaymentVnpay module
///   is added (see DECISION log about VNPay missing).
/// </summary>
public static class PaymentWebhookEndpoints
{
    public static IEndpointRouteBuilder MapPaymentWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .DisableAntiforgery();

        group.MapPost("/stripe", StripeWebhook);
        group.MapPost("/paypal", PaypalWebhook);
        group.MapPost("/momo", MomoWebhook);

        return app;
    }

    // Phase 3.7 stub — returns Accepted so providers stop retrying while the real
    // signature verification + domain update is wired in a follow-up commit.
    private static Accepted<string> StripeWebhook(HttpContext ctx) =>
        TypedResults.Accepted("/api/webhooks/stripe", "stripe webhook received — verification pending");

    private static Accepted<string> PaypalWebhook(HttpContext ctx) =>
        TypedResults.Accepted("/api/webhooks/paypal", "paypal webhook received — verification pending");

    private static Accepted<string> MomoWebhook(HttpContext ctx) =>
        TypedResults.Accepted("/api/webhooks/momo", "momo webhook received — verification pending");
}

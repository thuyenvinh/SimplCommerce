using Microsoft.AspNetCore.Http.HttpResults;

namespace SimplCommerce.ApiService.Webhooks;

/// <summary>
/// Payment gateway webhook callbacks. Each provider has its own signing scheme — the
/// actual HMAC / REST-verify logic lives in <see cref="WebhookSignatureVerifier"/>
/// and <see cref="PayPalWebhookVerifier"/> so the endpoints stay thin.
///
/// Contract per provider:
/// 1. Verify the signature / IPN first; reject anything that doesn't match with 401.
/// 2. Return 503 when the webhook secret isn't configured yet (ops can tell providers
///    to stop retrying while it's wired up).
/// 3. Endpoints are idempotent — providers retry freely.
/// 4. No auth middleware — the signature IS the authentication.
///
/// Domain side-effects (marking payments Captured / Refunded, firing OrderPaid) are
/// intentionally still stubs; they'll be wired per-provider once the Payments module
/// exposes the right command handlers. That follow-up is strictly additive — once
/// verification fails, no domain change happens, so it's safe to ship the gate alone.
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

    private static async Task<IResult> StripeWebhook(
        HttpContext ctx,
        IWebhookSignatureVerifier verifier,
        TimeProvider time)
    {
        var payload = await ReadBodyAsync(ctx);
        var signature = ctx.Request.Headers["Stripe-Signature"].ToString();
        var result = verifier.VerifyStripe(payload, signature, time.GetUtcNow());
        return Respond(result, "stripe");
    }

    private static async Task<IResult> PaypalWebhook(
        HttpContext ctx,
        IPayPalWebhookVerifier verifier,
        CancellationToken ct)
    {
        var payload = await ReadBodyAsync(ctx);
        var headers = ctx.Request.Headers.ToDictionary(
            kv => kv.Key.ToLowerInvariant(),
            kv => kv.Value.ToString());
        var result = await verifier.VerifyAsync(payload, headers, ct);
        return Respond(result, "paypal");
    }

    private static async Task<IResult> MomoWebhook(
        HttpContext ctx,
        IWebhookSignatureVerifier verifier)
    {
        var payload = await ReadBodyAsync(ctx);
        var result = verifier.VerifyMomo(payload);
        return Respond(result, "momo");
    }

    private static async Task<string> ReadBodyAsync(HttpContext ctx)
    {
        ctx.Request.EnableBuffering();
        using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        ctx.Request.Body.Position = 0;
        return body;
    }

    private static IResult Respond(WebhookVerifyResult result, string provider) => result switch
    {
        WebhookVerifyResult.Verified => TypedResults.Accepted(
            $"/api/webhooks/{provider}", $"{provider} webhook verified"),
        WebhookVerifyResult.NotConfigured => TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable),
        WebhookVerifyResult.Replay => TypedResults.Unauthorized(),
        WebhookVerifyResult.MalformedPayload => TypedResults.BadRequest(),
        _ => TypedResults.Unauthorized(),
    };
}

public static class WebhookServiceCollectionExtensions
{
    public static IServiceCollection AddWebhookVerifiers(this IServiceCollection services, IConfiguration configuration)
    {
        var stripe = configuration.GetSection(StripeWebhookOptions.SectionName).Get<StripeWebhookOptions>() ?? new();
        var paypal = configuration.GetSection(PayPalWebhookOptions.SectionName).Get<PayPalWebhookOptions>() ?? new();
        var momo = configuration.GetSection(MomoWebhookOptions.SectionName).Get<MomoWebhookOptions>() ?? new();

        services.AddSingleton(stripe);
        services.AddSingleton(paypal);
        services.AddSingleton(momo);
        services.AddSingleton<IWebhookSignatureVerifier>(_ => new WebhookSignatureVerifier(stripe, momo));
        services.AddHttpClient<IPayPalWebhookVerifier, PayPalWebhookVerifier>();
        return services;
    }
}

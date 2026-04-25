using System.Diagnostics;

namespace SimplCommerce.ApiService.Hardening;

/// <summary>
/// Writes a stable correlation id to <c>HttpContext.TraceIdentifier</c>, the response
/// <c>X-Correlation-Id</c> header, and the current <see cref="Activity"/> so every log
/// record + outbound HTTP call carries it.
/// Client-supplied <c>X-Correlation-Id</c> is honoured when present (idempotency +
/// tracing across service boundaries), otherwise we mint a new GUID.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers.TryGetValue(HeaderName, out var fromClient)
            && !string.IsNullOrWhiteSpace(fromClient)
                ? fromClient.ToString()
                : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = id;
        context.Response.Headers[HeaderName] = id;
        Activity.Current?.SetTag("simpl.correlation_id", id);

        using (context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Correlation")
            .BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = id }))
        {
            await next(context);
        }
    }
}

/// <summary>
/// Minimal set of response security headers. CSP is intentionally strict (no inline
/// script) — Blazor's blob: and self already cover the framework. Review the policy
/// before relaxing for 3rd-party analytics / payment iframes.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var h = context.Response.Headers;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"] = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), microphone=(), payment=()";
        h["Content-Security-Policy"] =
            "default-src 'self'; " +
            "img-src 'self' data: blob: https:; " +
            "script-src 'self' 'wasm-unsafe-eval' blob:; " +
            "style-src 'self' 'unsafe-inline'; " +
            "connect-src 'self' wss: https:; " +
            "font-src 'self' data:; " +
            "frame-ancestors 'none';";

        await next(context);
    }
}

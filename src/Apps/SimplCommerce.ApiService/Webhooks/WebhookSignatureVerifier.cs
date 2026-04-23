using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SimplCommerce.ApiService.Webhooks;

public enum WebhookVerifyResult
{
    Verified,
    NotConfigured,
    InvalidSignature,
    Replay,
    MalformedPayload
}

/// <summary>
/// Pure signature-verification primitives. Deliberately stateless / DI-friendly so the
/// endpoints stay thin and the algorithm is unit-testable with canonical vectors from
/// each provider's docs.
/// </summary>
public interface IWebhookSignatureVerifier
{
    WebhookVerifyResult VerifyStripe(string payload, string? signatureHeader, DateTimeOffset now);
    WebhookVerifyResult VerifyMomo(string payload);
}

public sealed class WebhookSignatureVerifier : IWebhookSignatureVerifier
{
    private readonly StripeWebhookOptions _stripe;
    private readonly MomoWebhookOptions _momo;

    public WebhookSignatureVerifier(StripeWebhookOptions stripe, MomoWebhookOptions momo)
    {
        _stripe = stripe;
        _momo = momo;
    }

    /// <summary>
    /// Stripe signature scheme: header <c>Stripe-Signature: t=&lt;unix&gt;,v1=&lt;hex&gt;[,v1=&lt;hex&gt;...]</c>.
    /// Signed payload is <c>&lt;timestamp&gt;.&lt;raw body&gt;</c> HMAC-SHA256 with the endpoint
    /// secret (whsec_...). Multiple v1 values are supported (rotation). See
    /// https://docs.stripe.com/webhooks#verify-manually.
    /// </summary>
    public WebhookVerifyResult VerifyStripe(string payload, string? signatureHeader, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(_stripe.WebhookSecret))
        {
            return WebhookVerifyResult.NotConfigured;
        }
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return WebhookVerifyResult.InvalidSignature;
        }

        long? timestamp = null;
        var v1Signatures = new List<string>();
        foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            var key = kv[0].Trim();
            var value = kv[1].Trim();
            if (key == "t" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ts))
            {
                timestamp = ts;
            }
            else if (key == "v1")
            {
                v1Signatures.Add(value);
            }
        }

        if (timestamp is null || v1Signatures.Count == 0)
        {
            return WebhookVerifyResult.InvalidSignature;
        }

        var age = Math.Abs(now.ToUnixTimeSeconds() - timestamp.Value);
        if (age > _stripe.ToleranceSeconds)
        {
            return WebhookVerifyResult.Replay;
        }

        var signedPayload = $"{timestamp.Value}.{payload}";
        var expected = HmacHex(_stripe.WebhookSecret, signedPayload);
        foreach (var sig in v1Signatures)
        {
            if (FixedTimeEquals(expected, sig))
            {
                return WebhookVerifyResult.Verified;
            }
        }
        return WebhookVerifyResult.InvalidSignature;
    }

    /// <summary>
    /// MoMo IPN verification: body is JSON, concatenate fields in a provider-specified order
    /// (see https://developers.momo.vn/v3/docs/payment/api/wallet/onetime#ipn-url) and HMAC-SHA256
    /// with the merchant SecretKey. Compare against <c>signature</c> field in the payload.
    /// </summary>
    public WebhookVerifyResult VerifyMomo(string payload)
    {
        if (string.IsNullOrWhiteSpace(_momo.SecretKey) || string.IsNullOrWhiteSpace(_momo.AccessKey))
        {
            return WebhookVerifyResult.NotConfigured;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            return WebhookVerifyResult.MalformedPayload;
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("signature", out var sigProp) ||
                sigProp.ValueKind != JsonValueKind.String)
            {
                return WebhookVerifyResult.InvalidSignature;
            }
            var providedSignature = sigProp.GetString()!;

            // MoMo v3 IPN signed-field order (strict — mismatched order = invalid signature):
            var ordered = new[]
            {
                "accessKey", "amount", "extraData", "message", "orderId",
                "orderInfo", "orderType", "partnerCode", "payType",
                "requestId", "responseTime", "resultCode", "transId"
            };

            var sb = new StringBuilder();
            var first = true;
            foreach (var field in ordered)
            {
                sb.Append(first ? "" : "&");
                sb.Append(field).Append('=');
                if (field == "accessKey")
                {
                    sb.Append(_momo.AccessKey);
                }
                else if (doc.RootElement.TryGetProperty(field, out var el))
                {
                    sb.Append(el.ValueKind switch
                    {
                        JsonValueKind.String => el.GetString(),
                        JsonValueKind.Number => el.GetRawText(),
                        _ => string.Empty,
                    });
                }
                first = false;
            }

            var expected = HmacHex(_momo.SecretKey, sb.ToString());
            return FixedTimeEquals(expected, providedSignature)
                ? WebhookVerifyResult.Verified
                : WebhookVerifyResult.InvalidSignature;
        }
    }

    public static string HmacHex(string secret, string message)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(a.ToLowerInvariant()),
            Encoding.ASCII.GetBytes(b.ToLowerInvariant()));
    }
}

/// <summary>
/// PayPal v2 webhooks require a REST roundtrip to <c>/v1/notifications/verify-webhook-signature</c>.
/// Abstracted behind an interface so unit tests can stub it; production impl uses HttpClient.
/// </summary>
public interface IPayPalWebhookVerifier
{
    Task<WebhookVerifyResult> VerifyAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken ct);
}

public sealed class PayPalWebhookVerifier : IPayPalWebhookVerifier
{
    private readonly PayPalWebhookOptions _options;
    private readonly HttpClient _http;

    public PayPalWebhookVerifier(PayPalWebhookOptions options, HttpClient http)
    {
        _options = options;
        _http = http;
    }

    public async Task<WebhookVerifyResult> VerifyAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookId) ||
            string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            return WebhookVerifyResult.NotConfigured;
        }

        string Header(string name) => headers.TryGetValue(name, out var v) ? v : string.Empty;

        var required = new[] { "paypal-transmission-id", "paypal-transmission-time", "paypal-transmission-sig", "paypal-cert-url", "paypal-auth-algo" };
        foreach (var h in required)
        {
            if (string.IsNullOrWhiteSpace(Header(h)))
            {
                return WebhookVerifyResult.InvalidSignature;
            }
        }

        JsonDocument bodyDoc;
        try { bodyDoc = JsonDocument.Parse(payload); }
        catch (JsonException) { return WebhookVerifyResult.MalformedPayload; }
        using var _ = bodyDoc;

        var host = _options.IsSandbox ? "api-m.sandbox.paypal.com" : "api-m.paypal.com";

        // 1. Get an OAuth token.
        using var tokenReq = new HttpRequestMessage(HttpMethod.Post, $"https://{host}/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("grant_type", "client_credentials") })
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        tokenReq.Headers.TryAddWithoutValidation("Authorization", $"Basic {basic}");
        using var tokenResp = await _http.SendAsync(tokenReq, ct);
        if (!tokenResp.IsSuccessStatusCode) return WebhookVerifyResult.InvalidSignature;
        using var tokenJson = JsonDocument.Parse(await tokenResp.Content.ReadAsStringAsync(ct));
        var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();

        // 2. Ask PayPal to verify.
        var verifyBody = JsonSerializer.Serialize(new
        {
            auth_algo = Header("paypal-auth-algo"),
            cert_url = Header("paypal-cert-url"),
            transmission_id = Header("paypal-transmission-id"),
            transmission_sig = Header("paypal-transmission-sig"),
            transmission_time = Header("paypal-transmission-time"),
            webhook_id = _options.WebhookId,
            webhook_event = JsonSerializer.Deserialize<JsonElement>(payload)
        });
        using var verifyReq = new HttpRequestMessage(HttpMethod.Post, $"https://{host}/v1/notifications/verify-webhook-signature")
        {
            Content = new StringContent(verifyBody, Encoding.UTF8, "application/json")
        };
        verifyReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        using var verifyResp = await _http.SendAsync(verifyReq, ct);
        if (!verifyResp.IsSuccessStatusCode) return WebhookVerifyResult.InvalidSignature;
        using var verifyJson = JsonDocument.Parse(await verifyResp.Content.ReadAsStringAsync(ct));
        var status = verifyJson.RootElement.GetProperty("verification_status").GetString();
        return status == "SUCCESS" ? WebhookVerifyResult.Verified : WebhookVerifyResult.InvalidSignature;
    }
}

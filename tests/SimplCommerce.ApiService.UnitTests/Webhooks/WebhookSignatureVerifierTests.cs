using System.Security.Cryptography;
using System.Text;
using SimplCommerce.ApiService.Webhooks;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Webhooks;

public class WebhookSignatureVerifierTests
{
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

    private static WebhookSignatureVerifier Create(string stripeSecret = "whsec_test", string momoSecret = "momo_secret", string momoAccess = "momo_access")
        => new(
            new StripeWebhookOptions { WebhookSecret = stripeSecret },
            new MomoWebhookOptions { SecretKey = momoSecret, AccessKey = momoAccess });

    private static string StripeSign(string secret, long ts, string body)
        => WebhookSignatureVerifier.HmacHex(secret, $"{ts}.{body}");

    [Fact]
    public void Stripe_verifies_valid_signature()
    {
        var body = "{\"id\":\"evt_123\",\"type\":\"payment_intent.succeeded\"}";
        var ts = FixedNow.ToUnixTimeSeconds();
        var sig = StripeSign("whsec_test", ts, body);

        var result = Create().VerifyStripe(body, $"t={ts},v1={sig}", FixedNow);

        Assert.Equal(WebhookVerifyResult.Verified, result);
    }

    [Fact]
    public void Stripe_accepts_multiple_v1_rotations()
    {
        var body = "{\"id\":\"evt_123\"}";
        var ts = FixedNow.ToUnixTimeSeconds();
        var validSig = StripeSign("whsec_test", ts, body);
        var header = $"t={ts},v1=deadbeef,v1={validSig}";

        var result = Create().VerifyStripe(body, header, FixedNow);

        Assert.Equal(WebhookVerifyResult.Verified, result);
    }

    [Fact]
    public void Stripe_rejects_tampered_body()
    {
        var ts = FixedNow.ToUnixTimeSeconds();
        var sigForOriginal = StripeSign("whsec_test", ts, "{\"amount\":100}");

        var result = Create().VerifyStripe("{\"amount\":9999}", $"t={ts},v1={sigForOriginal}", FixedNow);

        Assert.Equal(WebhookVerifyResult.InvalidSignature, result);
    }

    [Fact]
    public void Stripe_rejects_expired_timestamp()
    {
        var body = "{}";
        var oldTs = FixedNow.ToUnixTimeSeconds() - 600;
        var sig = StripeSign("whsec_test", oldTs, body);

        var result = Create().VerifyStripe(body, $"t={oldTs},v1={sig}", FixedNow);

        Assert.Equal(WebhookVerifyResult.Replay, result);
    }

    [Fact]
    public void Stripe_returns_not_configured_when_secret_missing()
    {
        var verifier = new WebhookSignatureVerifier(new StripeWebhookOptions(), new MomoWebhookOptions());

        var result = verifier.VerifyStripe("{}", "t=1,v1=x", FixedNow);

        Assert.Equal(WebhookVerifyResult.NotConfigured, result);
    }

    [Fact]
    public void Stripe_rejects_header_without_timestamp_or_v1()
    {
        var verifier = Create();

        Assert.Equal(WebhookVerifyResult.InvalidSignature, verifier.VerifyStripe("{}", null, FixedNow));
        Assert.Equal(WebhookVerifyResult.InvalidSignature, verifier.VerifyStripe("{}", "v1=abc", FixedNow));
        Assert.Equal(WebhookVerifyResult.InvalidSignature, verifier.VerifyStripe("{}", "t=1", FixedNow));
    }

    [Fact]
    public void Momo_verifies_valid_signature()
    {
        var access = "momo_access";
        var secret = "momo_secret";
        var fields = new Dictionary<string, string>
        {
            ["amount"] = "50000",
            ["extraData"] = "",
            ["message"] = "Success",
            ["orderId"] = "ord-1",
            ["orderInfo"] = "Pay for order ord-1",
            ["orderType"] = "momo_wallet",
            ["partnerCode"] = "MOMO",
            ["payType"] = "qr",
            ["requestId"] = "req-1",
            ["responseTime"] = "1700000000000",
            ["resultCode"] = "0",
            ["transId"] = "99999",
        };
        var ordered = new[] { "accessKey", "amount", "extraData", "message", "orderId", "orderInfo", "orderType", "partnerCode", "payType", "requestId", "responseTime", "resultCode", "transId" };
        var sb = new StringBuilder();
        var first = true;
        foreach (var f in ordered)
        {
            sb.Append(first ? "" : "&");
            sb.Append(f).Append('=').Append(f == "accessKey" ? access : fields[f]);
            first = false;
        }
        var expectedSig = WebhookSignatureVerifier.HmacHex(secret, sb.ToString());

        var body = $"{{\"partnerCode\":\"{fields["partnerCode"]}\",\"orderId\":\"{fields["orderId"]}\",\"requestId\":\"{fields["requestId"]}\",\"amount\":{fields["amount"]},\"orderInfo\":\"{fields["orderInfo"]}\",\"orderType\":\"{fields["orderType"]}\",\"transId\":{fields["transId"]},\"resultCode\":{fields["resultCode"]},\"message\":\"{fields["message"]}\",\"payType\":\"{fields["payType"]}\",\"responseTime\":{fields["responseTime"]},\"extraData\":\"{fields["extraData"]}\",\"signature\":\"{expectedSig}\"}}";

        var result = Create(momoSecret: secret, momoAccess: access).VerifyMomo(body);

        Assert.Equal(WebhookVerifyResult.Verified, result);
    }

    [Fact]
    public void Momo_rejects_bad_signature()
    {
        var body = "{\"amount\":50000,\"orderId\":\"ord-1\",\"signature\":\"deadbeef\"}";

        var result = Create().VerifyMomo(body);

        Assert.Equal(WebhookVerifyResult.InvalidSignature, result);
    }

    [Fact]
    public void Momo_returns_not_configured_when_secret_missing()
    {
        var verifier = new WebhookSignatureVerifier(new StripeWebhookOptions(), new MomoWebhookOptions());

        var result = verifier.VerifyMomo("{}");

        Assert.Equal(WebhookVerifyResult.NotConfigured, result);
    }

    [Fact]
    public void Momo_returns_malformed_on_invalid_json()
    {
        var result = Create().VerifyMomo("not-json");

        Assert.Equal(WebhookVerifyResult.MalformedPayload, result);
    }

    [Fact]
    public void Momo_returns_invalid_signature_when_field_missing()
    {
        var result = Create().VerifyMomo("{\"amount\":100}");

        Assert.Equal(WebhookVerifyResult.InvalidSignature, result);
    }
}

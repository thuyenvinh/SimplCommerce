namespace SimplCommerce.ApiService.Webhooks;

public class StripeWebhookOptions
{
    public const string SectionName = "Payments:Stripe";

    /// <summary>Shared secret (whsec_...) configured when creating the endpoint in Stripe dashboard.</summary>
    public string? WebhookSecret { get; set; }

    /// <summary>Replay-attack window. Stripe recommends 5 minutes.</summary>
    public int ToleranceSeconds { get; set; } = 300;
}

public class PayPalWebhookOptions
{
    public const string SectionName = "Payments:PayPal";

    public bool IsSandbox { get; set; } = true;

    /// <summary>Webhook ID from PayPal dashboard (used for v2 REST signature verification).</summary>
    public string? WebhookId { get; set; }

    /// <summary>REST API credentials for the /v1/notifications/verify-webhook-signature call.</summary>
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

public class MomoWebhookOptions
{
    public const string SectionName = "Payments:MoMo";

    public string? PartnerCode { get; set; }
    public string? AccessKey { get; set; }

    /// <summary>Shared secret used to HMAC-SHA256 the sorted IPN payload.</summary>
    public string? SecretKey { get; set; }
}

#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace SimplCommerce.Module.PaymentStripe.Services;

/// <summary>
/// Wave 12: Stripe Connect payouts. Calls
/// <c>POST /v1/transfers</c> with <c>destination = vendor.StripeAccountId</c>
/// so the platform pushes the vendor's net amount to their connected account
/// after withholding the commission.
///
/// Configuration (appsettings or env):
///   Stripe:SecretKey = sk_test_... (or sk_live_...)
///
/// If the key is missing the service returns a "not configured" result and the
/// payout endpoint parks the payout at Failed for admin retry — this is the
/// safe stub mode used in CI + integration tests where no real key is wired.
///
/// All amounts are in the gateway's minor units (Stripe uses cents). We pass
/// the platform's <c>VendorPayoutId</c> as the <c>idempotency_key</c> so
/// retries (admin re-clicks "send", network blip, etc.) don't double-transfer.
/// </summary>
public interface IStripeConnectService
{
    Task<StripeTransferResult> CreateTransferAsync(
        long payoutId,
        string destinationAccountId,
        decimal amount,
        string currency,
        string description,
        CancellationToken ct);
}

public sealed record StripeTransferResult(
    bool Success,
    string? TransferId,
    string? FailureCode,
    string? RawResponse);

public sealed class StripeConnectOptions
{
    public const string SectionName = "Stripe";
    public string SecretKey { get; init; } = string.Empty;
    public string Currency { get; init; } = "usd";
}

public sealed class StripeConnectService : IStripeConnectService
{
    private readonly StripeConnectOptions _options;
    private readonly ILogger<StripeConnectService> _logger;

    public StripeConnectService(IOptions<StripeConnectOptions> options, ILogger<StripeConnectService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<StripeTransferResult> CreateTransferAsync(
        long payoutId,
        string destinationAccountId,
        decimal amount,
        string currency,
        string description,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogWarning("Stripe:SecretKey not configured — payout {PayoutId} will park at Failed.", payoutId);
            return new StripeTransferResult(false, null, "stripe_not_configured", null);
        }
        if (string.IsNullOrWhiteSpace(destinationAccountId))
        {
            return new StripeTransferResult(false, null, "missing_destination", null);
        }

        // Stripe.net is mostly stateless — set the API key on the static
        // configuration once per call so a config reload propagates without
        // re-instantiating the service. RequestOptions could carry per-call
        // keys but it adds boilerplate to every method.
        StripeConfiguration.ApiKey = _options.SecretKey;

        var requestOptions = new RequestOptions
        {
            // Idempotency: same payout id => Stripe returns the original transfer
            // instead of creating a duplicate. Critical when admin retries from
            // Failed → Pending → Sent and we don't want two physical transfers.
            IdempotencyKey = $"vendor-payout-{payoutId}",
        };
        var createOptions = new TransferCreateOptions
        {
            Amount = (long)System.Math.Round(amount * 100m, 0), // minor units
            Currency = string.IsNullOrWhiteSpace(currency) ? _options.Currency : currency,
            Destination = destinationAccountId,
            Description = description,
            Metadata = new System.Collections.Generic.Dictionary<string, string>
            {
                ["payoutId"] = payoutId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            },
        };

        try
        {
            var service = new TransferService();
            var transfer = await service.CreateAsync(createOptions, requestOptions, ct);
            _logger.LogInformation("Stripe transfer {TransferId} succeeded for payout {PayoutId}.", transfer.Id, payoutId);
            return new StripeTransferResult(true, transfer.Id, null, transfer.StripeResponse?.Content);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe transfer failed for payout {PayoutId}: {Code}.", payoutId, ex.StripeError?.Code);
            return new StripeTransferResult(false, null, ex.StripeError?.Code ?? "stripe_error", ex.StripeError?.Message ?? ex.Message);
        }
    }
}

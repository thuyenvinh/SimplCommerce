#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace SimplCommerce.Module.Payments.Services;

/// <summary>
/// Wave 12: rail-agnostic payout abstraction. Each provider (Stripe Connect,
/// VNPay vendor sub-account, MoMo partner) registers an implementation keyed by
/// the matching <c>VendorPayout.Method</c> enum value. The Vendors module picks
/// the right gateway by Method at settlement time without taking a direct
/// dependency on any payment-provider package.
/// </summary>
public interface IPayoutGateway
{
    /// <summary>Which <c>VendorPayout.Method</c> enum value this gateway services.</summary>
    int MethodId { get; }

    Task<PayoutDispatchResult> DispatchAsync(PayoutDispatchRequest request, CancellationToken ct);
}

public sealed record PayoutDispatchRequest(
    long PayoutId,
    string DestinationAccountId,
    decimal NetAmount,
    string Currency,
    string Description);

public sealed record PayoutDispatchResult(
    bool Success,
    string? ProviderReference,
    string? FailureCode,
    string? RawResponse);

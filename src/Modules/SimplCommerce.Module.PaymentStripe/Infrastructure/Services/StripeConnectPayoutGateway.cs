#nullable enable
using System.Threading;
using System.Threading.Tasks;
using SimplCommerce.Module.Payments.Services;

namespace SimplCommerce.Module.PaymentStripe.Services;

/// <summary>
/// Wave 12: adapts <see cref="IStripeConnectService"/> to the rail-agnostic
/// <see cref="IPayoutGateway"/> contract. MethodId 5 matches
/// <c>VendorPayout.PayoutMethod.StripeConnect</c>.
/// </summary>
public sealed class StripeConnectPayoutGateway : IPayoutGateway
{
    private readonly IStripeConnectService _stripe;

    public StripeConnectPayoutGateway(IStripeConnectService stripe)
    {
        _stripe = stripe;
    }

    // 5 = PayoutMethod.StripeConnect; keep in sync with the enum or pull it
    // via a referenced constant if a follow-up extracts the enum to a shared
    // location.
    public int MethodId => 5;

    public async Task<PayoutDispatchResult> DispatchAsync(PayoutDispatchRequest request, CancellationToken ct)
    {
        var result = await _stripe.CreateTransferAsync(
            request.PayoutId,
            request.DestinationAccountId,
            request.NetAmount,
            request.Currency,
            request.Description,
            ct);
        return new PayoutDispatchResult(result.Success, result.TransferId, result.FailureCode, result.RawResponse);
    }
}

namespace SimplCommerce.Module.PaymentVnpay;

/// <summary>
/// Settings stored on the PaymentProvider row (JSON in AdditionalSettings) for VNPAY
/// merchant integration. The pay-now URL is provider-fixed; everything else comes from
/// the VNPAY merchant portal.
/// </summary>
public class VnpayConfig
{
    public bool IsSandbox { get; set; } = true;

    public string TmnCode { get; set; } = string.Empty;

    /// <summary>Hash secret used to HMAC-SHA512 the query string.</summary>
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>URL the buyer is redirected to in order to pay.</summary>
    public string PayUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    public decimal PaymentFee { get; set; }
}

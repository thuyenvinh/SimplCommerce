using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SimplCommerce.Module.PaymentVnpay;

/// <summary>
/// VNPAY signs requests and verifies IPN/return URLs with HMAC-SHA512 over a
/// sorted query string of <c>vnp_*</c> parameters (excluding <c>vnp_SecureHash</c>
/// itself). Same algorithm both directions; tiny pure helper so the route handler
/// stays trivial and the unit tests can exercise it with canonical vectors.
///
/// Spec: https://sandbox.vnpayment.vn/apis/docs/huong-dan-tich-hop/
/// </summary>
public static class VnpaySignature
{
    public static string Build(IReadOnlyDictionary<string, string> parameters, string hashSecret)
    {
        var sorted = parameters
            .Where(kv => kv.Key.StartsWith("vnp_", StringComparison.Ordinal) && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        var first = true;
        foreach (var kv in sorted)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;
            if (!first) sb.Append('&');
            sb.Append(WebEncode(kv.Key)).Append('=').Append(WebEncode(kv.Value));
            first = false;
        }

        return HmacSha512Hex(hashSecret, sb.ToString());
    }

    public static bool Verify(IReadOnlyDictionary<string, string> parameters, string hashSecret)
    {
        if (!parameters.TryGetValue("vnp_SecureHash", out var provided) || string.IsNullOrEmpty(provided))
        {
            return false;
        }
        var expected = Build(parameters, hashSecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected.ToLowerInvariant()),
            Encoding.ASCII.GetBytes(provided.ToLowerInvariant()));
    }

    private static string HmacSha512Hex(string secret, string message)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string WebEncode(string raw) =>
        System.Net.WebUtility.UrlEncode(raw) ?? string.Empty;

    public static string FormatAmount(decimal amount) =>
        ((long)(amount * 100m)).ToString(CultureInfo.InvariantCulture);
}

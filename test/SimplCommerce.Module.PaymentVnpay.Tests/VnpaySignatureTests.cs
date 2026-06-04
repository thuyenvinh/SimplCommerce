using SimplCommerce.Module.PaymentVnpay;
using Xunit;

namespace SimplCommerce.Module.PaymentVnpay.Tests;

public class VnpaySignatureTests
{
    private const string Secret = "TESTSECRETKEY1234567890";

    [Fact]
    public void Sorts_parameters_alphabetically_before_hashing()
    {
        var unordered = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = "abc",
            ["vnp_Amount"] = "1000000",
            ["vnp_Command"] = "pay",
        };
        var ordered = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_Command"] = "pay",
            ["vnp_TxnRef"] = "abc",
        };

        Assert.Equal(VnpaySignature.Build(ordered, Secret), VnpaySignature.Build(unordered, Secret));
    }

    [Fact]
    public void Skips_secure_hash_fields_when_signing()
    {
        var withSecureHash = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_TxnRef"] = "abc",
            ["vnp_SecureHash"] = "deadbeef",
            ["vnp_SecureHashType"] = "SHA512",
        };
        var without = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_TxnRef"] = "abc",
        };

        Assert.Equal(VnpaySignature.Build(without, Secret), VnpaySignature.Build(withSecureHash, Secret));
    }

    [Fact]
    public void Verify_round_trips_a_valid_signature()
    {
        var parameters = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_TxnRef"] = "abc",
        };
        var signature = VnpaySignature.Build(parameters, Secret);
        parameters["vnp_SecureHash"] = signature;

        Assert.True(VnpaySignature.Verify(parameters, Secret));
    }

    [Fact]
    public void Verify_rejects_tampered_amount()
    {
        var original = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_TxnRef"] = "abc",
        };
        var signature = VnpaySignature.Build(original, Secret);
        var tampered = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "9999900",
            ["vnp_TxnRef"] = "abc",
            ["vnp_SecureHash"] = signature,
        };

        Assert.False(VnpaySignature.Verify(tampered, Secret));
    }

    [Fact]
    public void Verify_rejects_when_signature_field_missing()
    {
        var parameters = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_TxnRef"] = "abc",
        };

        Assert.False(VnpaySignature.Verify(parameters, Secret));
    }

    [Fact]
    public void Formats_amount_in_hundredths_of_a_unit()
    {
        Assert.Equal("100000", VnpaySignature.FormatAmount(1000m));
        Assert.Equal("123456", VnpaySignature.FormatAmount(1234.56m));
    }

    [Fact]
    public void Ignores_non_vnp_prefixed_parameters()
    {
        var withExtra = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1000",
            ["unrelated"] = "ignored",
            ["another_one"] = "ignored",
        };
        var clean = new Dictionary<string, string> { ["vnp_Amount"] = "1000" };

        Assert.Equal(VnpaySignature.Build(clean, Secret), VnpaySignature.Build(withExtra, Secret));
    }
}

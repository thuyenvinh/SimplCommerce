using FluentAssertions;
using SimplCommerce.Module.Payments.Models;
using Xunit;

namespace SimplCommerce.Module.Payments.Tests.Domain;

public class PaymentTests
{
    [Fact]
    public void New_payment_stamps_created_and_updated_timestamps()
    {
        var before = DateTimeOffset.Now.AddSeconds(-1);
        var p = new Payment();
        var after = DateTimeOffset.Now.AddSeconds(1);

        p.CreatedOn.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        p.LatestUpdatedOn.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Status_succeeded_round_trips()
    {
        var p = new Payment { Status = PaymentStatus.Succeeded };
        p.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public void Status_failed_records_failure_message()
    {
        var p = new Payment { Status = PaymentStatus.Failed, FailureMessage = "insufficient_funds" };
        p.Status.Should().Be(PaymentStatus.Failed);
        p.FailureMessage.Should().Be("insufficient_funds");
    }

    [Fact]
    public void Amount_and_fee_round_trip()
    {
        var p = new Payment { Amount = 199.99m, PaymentFee = 3.50m };
        p.Amount.Should().Be(199.99m);
        p.PaymentFee.Should().Be(3.50m);
    }
}

public class PaymentStatusTests
{
    [Theory]
    [InlineData(PaymentStatus.Succeeded, 1)]
    [InlineData(PaymentStatus.Failed, 5)]
    public void Enum_values_are_stable(PaymentStatus s, int expected) =>
        ((int)s).Should().Be(expected);
}

public class PaymentProviderTests
{
    [Fact]
    public void Constructor_sets_id()
    {
        var p = new PaymentProvider("stripe");
        p.Id.Should().Be("stripe");
    }

    [Fact]
    public void Disabled_by_default()
    {
        new PaymentProvider("cod").IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Can_store_additional_settings_as_json_string()
    {
        var json = "{\"webhookSecret\":\"whsec_...\"}";
        var p = new PaymentProvider("stripe") { AdditionalSettings = json };
        p.AdditionalSettings.Should().Be(json);
    }
}

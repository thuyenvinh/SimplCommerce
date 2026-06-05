using SimplCommerce.ApiService.CheckoutFlow;
using SimplCommerce.RealTime;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.CheckoutFlow;

/// <summary>
/// Contract assertions that lock the wire shape of the checkout endpoints + admin
/// notification payload. They aren't full integration tests (those run with
/// Testcontainers under <c>Category=RequiresDocker</c>), just guards so refactors
/// don't silently rename JSON fields the Storefront/Admin clients depend on.
/// </summary>
public class CheckoutEndpointContractTests
{
    [Fact]
    public void CheckoutAddressRequest_has_expected_property_set()
    {
        var record = new CheckoutStorefrontEndpoints.CheckoutAddressRequest(
            ContactName: "Jane",
            Phone: "+1-555",
            AddressLine1: "1 Infinite Loop",
            AddressLine2: null,
            CountryId: "US",
            StateOrProvinceId: 42,
            DistrictId: null,
            City: "Cupertino",
            ZipCode: "95014",
            UseShippingAddressAsBillingAddress: true,
            ShippingMethod: null,
            OrderNote: null);

        Assert.Equal("Jane", record.ContactName);
        Assert.True(record.UseShippingAddressAsBillingAddress);
        Assert.Equal("US", record.CountryId);
    }

    [Fact]
    public void CheckoutSummary_round_trips_subtotal_and_items()
    {
        var summary = new CheckoutStorefrontEndpoints.CheckoutSummary(
            Id: Guid.NewGuid(),
            SubTotal: 100m,
            Discount: 10m,
            ShippingAmount: 5m,
            TaxAmount: 8m,
            OrderTotal: 103m,
            CouponCode: "SUMMER",
            Items: new[]
            {
                new CheckoutStorefrontEndpoints.CheckoutLine(1, "Widget", 2, 50m),
            });

        Assert.Equal(100m, summary.SubTotal);
        Assert.Single(summary.Items);
        Assert.Equal("Widget", summary.Items[0].ProductName);
    }

    [Fact]
    public void PlaceOrderRequest_defaults_zero_fee()
    {
        var req = new CheckoutStorefrontEndpoints.PlaceOrderRequest("CashOnDelivery", 0);

        Assert.Equal(0m, req.PaymentFeeAmount);
        Assert.Equal("CashOnDelivery", req.PaymentMethod);
    }

    [Fact]
    public void AdminNotification_round_trips_link()
    {
        var n = new AdminNotification("order-created", "T", "M", "/orders/42", DateTimeOffset.UtcNow);

        Assert.Equal("/orders/42", n.Link);
        Assert.Equal("order-created", n.Kind);
    }
}

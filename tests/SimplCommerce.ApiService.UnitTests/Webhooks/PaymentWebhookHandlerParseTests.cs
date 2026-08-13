using FluentAssertions;
using SimplCommerce.ApiService.Webhooks;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Webhooks;

// G02: parse-only tests. The mutation paths (mark succeeded / failed / refunded)
// are exercised through the integration suite once a SQL container is available;
// here we pin the JSON-shape invariants because Stripe payload deviations are
// the most common source of webhook bugs.
public class PaymentWebhookHandlerParseTests
{
    [Fact]
    public void Parses_payment_intent_succeeded_with_id()
    {
        const string payload = """
        {
            "id": "evt_1",
            "type": "payment_intent.succeeded",
            "data": { "object": { "id": "pi_abc123", "amount": 5000, "currency": "usd" } }
        }
        """;

        var evt = PaymentWebhookHandler.ParseStripe(payload);

        evt.Should().NotBeNull();
        evt!.Type.Should().Be("payment_intent.succeeded");
        evt.PaymentIntentId.Should().Be("pi_abc123");
        evt.AmountRefunded.Should().Be(0m);
    }

    [Fact]
    public void Parses_charge_refunded_with_payment_intent_link_and_amount()
    {
        // Stripe refund events live on the Charge, not on the PaymentIntent —
        // we lift the payment_intent property up so the handler can find the
        // captured Payment by GatewayTransactionId. amount_refunded is in
        // minor units (cents).
        const string payload = """
        {
            "id": "evt_2",
            "type": "charge.refunded",
            "data": { "object": { "id": "ch_xyz", "payment_intent": "pi_abc123", "amount_refunded": 1250 } }
        }
        """;

        var evt = PaymentWebhookHandler.ParseStripe(payload);

        evt.Should().NotBeNull();
        evt!.Type.Should().Be("charge.refunded");
        evt.PaymentIntentId.Should().Be("pi_abc123");
        evt.AmountRefunded.Should().Be(12.50m);
    }

    [Fact]
    public void Parses_event_without_data_object_as_payload_minus_intent()
    {
        const string payload = """
        { "id": "evt_3", "type": "ping" }
        """;

        var evt = PaymentWebhookHandler.ParseStripe(payload);

        evt.Should().NotBeNull();
        evt!.Type.Should().Be("ping");
        evt.PaymentIntentId.Should().BeNull();
    }

    [Fact]
    public void Returns_null_when_type_missing()
    {
        const string payload = """{ "id": "evt_4" }""";

        var evt = PaymentWebhookHandler.ParseStripe(payload);

        evt.Should().BeNull();
    }

    [Fact]
    public void Unknown_event_types_still_parse_but_have_no_intent_link()
    {
        // Stripe ships dozens of event types; we forward unknown types to the
        // handler so the default branch can log + skip, but we shouldn't
        // accidentally extract an id from an unrelated object shape.
        const string payload = """
        {
            "type": "customer.created",
            "data": { "object": { "id": "cus_x" } }
        }
        """;

        var evt = PaymentWebhookHandler.ParseStripe(payload);

        evt.Should().NotBeNull();
        evt!.Type.Should().Be("customer.created");
        evt.PaymentIntentId.Should().BeNull();
    }
}

using System.Diagnostics.Metrics;

namespace SimplCommerce.ApiService.Hardening;

/// <summary>
/// Custom OpenTelemetry metrics — counters for the three storefront events the ops
/// team cares about. Aspire's dashboard + any OTLP-compatible collector
/// (Prometheus / Grafana) pick these up automatically because the meter name is
/// added via the OpenTelemetry tracer/meter builder in ServiceDefaults (see also
/// <see cref="Extensions"/>). Emit from the endpoint handlers that own the event.
/// </summary>
public sealed class SimplMetrics : IDisposable
{
    public const string MeterName = "SimplCommerce";

    private readonly Meter _meter;
    public Counter<long> OrdersCreated { get; }
    public Counter<long> PaymentsFailed { get; }
    public Counter<long> CartsAbandoned { get; }

    public SimplMetrics(IMeterFactory factory)
    {
        _meter = factory.Create(MeterName);
        OrdersCreated = _meter.CreateCounter<long>("simpl.orders.created",
            unit: "orders", description: "Orders successfully created");
        PaymentsFailed = _meter.CreateCounter<long>("simpl.payments.failed",
            unit: "payments", description: "Payments that returned a non-success status");
        CartsAbandoned = _meter.CreateCounter<long>("simpl.carts.abandoned",
            unit: "carts", description: "Carts that expired without checkout");
    }

    public void Dispose() => _meter.Dispose();
}

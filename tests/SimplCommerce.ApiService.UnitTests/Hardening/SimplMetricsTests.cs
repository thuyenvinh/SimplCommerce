using System.Diagnostics.Metrics;
using FluentAssertions;
using SimplCommerce.ApiService.Hardening;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Hardening;

public class SimplMetricsTests
{
    [Fact]
    public void Exposes_three_counters_under_SimplCommerce_meter()
    {
        using var factory = new TestMeterFactory();
        using var sut = new SimplMetrics(factory);

        sut.OrdersCreated.Name.Should().Be("simpl.orders.created");
        sut.PaymentsFailed.Name.Should().Be("simpl.payments.failed");
        sut.CartsAbandoned.Name.Should().Be("simpl.carts.abandoned");

        sut.OrdersCreated.Meter.Name.Should().Be(SimplMetrics.MeterName);
    }

    [Fact]
    public void Meter_name_constant_matches_registration_contract()
    {
        SimplMetrics.MeterName.Should().Be("SimplCommerce");
    }

    [Fact]
    public void Counters_record_deltas_via_listener()
    {
        using var factory = new TestMeterFactory();
        using var sut = new SimplMetrics(factory);

        var sum = 0L;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instr, l) =>
            {
                if (instr.Meter.Name == SimplMetrics.MeterName) l.EnableMeasurementEvents(instr);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => sum += value);
        listener.Start();

        sut.OrdersCreated.Add(1);
        sut.OrdersCreated.Add(2);
        sut.PaymentsFailed.Add(5);

        sum.Should().Be(8);
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = new();

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version, options.Tags, scope: this);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var m in _meters) m.Dispose();
        }
    }
}

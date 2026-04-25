using FluentAssertions;
using SimplCommerce.Module.Orders.Events;
using SimplCommerce.Module.Orders.Models;
using Xunit;

namespace SimplCommerce.Module.Orders.Tests.Events;

public class EventDtoTests
{
    [Fact]
    public void OrderCreated_carries_order_reference()
    {
        var order = new Order();
        var evt = new OrderCreated(order);
        evt.Order.Should().BeSameAs(order);
    }

    [Fact]
    public void AfterOrderCreated_carries_order_reference()
    {
        var order = new Order();
        var evt = new AfterOrderCreated(order);
        evt.Order.Should().BeSameAs(order);
    }

    [Fact]
    public void OrderChanged_default_status_transition_fields()
    {
        var evt = new OrderChanged
        {
            OrderId = 42,
            OldStatus = OrderStatus.PendingPayment,
            NewStatus = OrderStatus.PaymentReceived,
            UserId = 1,
            Note = "paid via card"
        };

        evt.OrderId.Should().Be(42);
        evt.OldStatus.Should().Be(OrderStatus.PendingPayment);
        evt.NewStatus.Should().Be(OrderStatus.PaymentReceived);
        evt.UserId.Should().Be(1);
        evt.Note.Should().Be("paid via card");
    }

    [Fact]
    public void OrderChanged_old_status_is_optional()
    {
        var evt = new OrderChanged { OrderId = 1, NewStatus = OrderStatus.New };
        evt.OldStatus.Should().BeNull();
    }
}

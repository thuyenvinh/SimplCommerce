using FluentAssertions;
using SimplCommerce.Module.Orders.Models;
using Xunit;

namespace SimplCommerce.Module.Orders.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Constructor_initializes_status_to_new()
    {
        var order = new Order();
        order.OrderStatus.Should().Be(OrderStatus.New);
    }

    [Fact]
    public void Constructor_initializes_timestamps()
    {
        var before = DateTimeOffset.Now.AddSeconds(-1);
        var order = new Order();
        var after = DateTimeOffset.Now.AddSeconds(1);

        order.CreatedOn.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        order.LatestUpdatedOn.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_sets_is_master_order_to_false()
    {
        new Order().IsMasterOrder.Should().BeFalse();
    }

    [Fact]
    public void Constructor_initializes_empty_children_collection()
    {
        var order = new Order();
        order.Children.Should().NotBeNull();
        order.Children.Should().BeEmpty();
    }

    [Fact]
    public void AddOrderItem_links_item_to_order_and_appends()
    {
        var order = new Order();
        var item = new OrderItem { ProductId = 1, Quantity = 2, ProductPrice = 9.99m };

        order.AddOrderItem(item);

        order.OrderItems.Should().HaveCount(1);
        order.OrderItems.Single().Should().BeSameAs(item);
        item.Order.Should().BeSameAs(order);
    }

    [Fact]
    public void AddOrderItem_accumulates_multiple_items()
    {
        var order = new Order();
        order.AddOrderItem(new OrderItem { ProductId = 1 });
        order.AddOrderItem(new OrderItem { ProductId = 2 });
        order.AddOrderItem(new OrderItem { ProductId = 3 });
        order.OrderItems.Select(i => i.ProductId).Should().Equal(1, 2, 3);
    }
}

public class OrderStatusTests
{
    [Theory]
    [InlineData(OrderStatus.New, 1)]
    [InlineData(OrderStatus.OnHold, 10)]
    [InlineData(OrderStatus.PendingPayment, 20)]
    [InlineData(OrderStatus.PaymentReceived, 30)]
    [InlineData(OrderStatus.PaymentFailed, 35)]
    [InlineData(OrderStatus.Invoiced, 40)]
    [InlineData(OrderStatus.Shipping, 50)]
    [InlineData(OrderStatus.Shipped, 60)]
    [InlineData(OrderStatus.Complete, 70)]
    [InlineData(OrderStatus.Canceled, 80)]
    [InlineData(OrderStatus.Refunded, 90)]
    [InlineData(OrderStatus.Closed, 100)]
    public void Enum_values_are_stable_for_database_compatibility(OrderStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }
}

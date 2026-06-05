using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using MockQueryable;
using Moq;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Pricing.Services;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;
using Xunit;

namespace SimplCommerce.Module.ShoppingCart.Tests.Services;

public class CartServiceTests
{
    private readonly Mock<IRepository<CartItem>> _repo = new();
    private readonly Mock<ICouponService> _coupon = new();
    private readonly Mock<IMediaService> _media = new();
    private readonly Mock<ICurrencyService> _currency = new();
    private readonly Mock<IProductPricingService> _pricing = new();
    private readonly IConfiguration _config = new ConfigurationBuilder().Build();
    private readonly IStringLocalizerFactory _localizerFactory = new StubLocalizerFactory();

    private CartService Build(params CartItem[] existingItems)
    {
        _repo.Setup(x => x.Query()).Returns(existingItems.AsQueryable().BuildMock());
        _repo.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        return new CartService(_repo.Object, _coupon.Object, _media.Object, _config, _currency.Object, _localizerFactory, _pricing.Object);
    }

    [Fact]
    public async Task Zero_quantity_returns_wrong_quantity_error()
    {
        var sut = Build();
        var result = await sut.AddToCart(customerId: 1, productId: 100, quantity: 0);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("wrong-quantity");
        _repo.Verify(x => x.Add(It.IsAny<CartItem>()), Times.Never);
        _repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Negative_quantity_returns_wrong_quantity_error()
    {
        var sut = Build();
        var result = await sut.AddToCart(customerId: 1, productId: 100, quantity: -3);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("wrong-quantity");
    }

    [Fact]
    public async Task First_add_creates_new_cart_item()
    {
        var sut = Build();
        CartItem? added = null;
        _repo.Setup(x => x.Add(It.IsAny<CartItem>())).Callback<CartItem>(ci => added = ci);

        var result = await sut.AddToCart(customerId: 42, productId: 7, quantity: 2);

        result.Success.Should().BeTrue();
        added.Should().NotBeNull();
        added!.CustomerId.Should().Be(42);
        added.ProductId.Should().Be(7);
        added.Quantity.Should().Be(2);
        _repo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Subsequent_add_increments_existing_quantity()
    {
        var existing = new CartItem { CustomerId = 42, ProductId = 7, Quantity = 3 };
        var sut = Build(existing);

        var result = await sut.AddToCart(customerId: 42, productId: 7, quantity: 5);

        result.Success.Should().BeTrue();
        existing.Quantity.Should().Be(8);
        _repo.Verify(x => x.Add(It.IsAny<CartItem>()), Times.Never);
        _repo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Different_customer_does_not_match_existing_item()
    {
        var existing = new CartItem { CustomerId = 42, ProductId = 7, Quantity = 3 };
        var sut = Build(existing);

        var result = await sut.AddToCart(customerId: 999, productId: 7, quantity: 1);

        result.Success.Should().BeTrue();
        existing.Quantity.Should().Be(3);
        _repo.Verify(x => x.Add(It.IsAny<CartItem>()), Times.Once);
    }

    private sealed class StubLocalizerFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => new StubLocalizer();
        public IStringLocalizer Create(string baseName, string location) => new StubLocalizer();
    }

    private sealed class StubLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
    }
}

public class AddToCartResultTests
{
    [Fact]
    public void Default_instance_is_unsuccessful_with_null_error_fields()
    {
        var r = new AddToCartResult();
        r.Success.Should().BeFalse();
        r.ErrorCode.Should().BeNull();
        r.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Properties_can_be_set()
    {
        var r = new AddToCartResult { Success = true, ErrorCode = "x", ErrorMessage = "y" };
        r.Success.Should().BeTrue();
        r.ErrorCode.Should().Be("x");
        r.ErrorMessage.Should().Be("y");
    }
}

using FluentAssertions;
using Moq;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Services;
using Xunit;

namespace SimplCommerce.Module.Catalog.Tests;

public class ProductPricingServiceTests
{
    private readonly ProductPricingService _sut = new(Mock.Of<ICurrencyService>());

    [Fact]
    public void Without_special_or_old_price_returns_plain_price()
    {
        var result = _sut.CalculateProductPrice(price: 100m, oldPrice: null, specialPrice: null,
            specialPriceStart: null, specialPriceEnd: null);

        result.Price.Should().Be(100m);
        result.OldPrice.Should().BeNull();
        result.PercentOfSaving.Should().Be(0);
    }

    [Fact]
    public void Old_price_higher_than_price_calculates_savings()
    {
        var result = _sut.CalculateProductPrice(price: 80m, oldPrice: 100m, specialPrice: null,
            specialPriceStart: null, specialPriceEnd: null);

        result.Price.Should().Be(80m);
        result.OldPrice.Should().Be(100m);
        result.PercentOfSaving.Should().Be(20);
    }

    [Fact]
    public void Old_price_lower_than_price_is_ignored()
    {
        var result = _sut.CalculateProductPrice(price: 100m, oldPrice: 50m, specialPrice: null,
            specialPriceStart: null, specialPriceEnd: null);

        // OldPrice lower than price isn't a sale — percent of saving stays 0.
        result.Price.Should().Be(100m);
        result.PercentOfSaving.Should().Be(0);
    }

    [Fact]
    public void Active_special_price_overrides_and_promotes_base_price_to_oldPrice()
    {
        var result = _sut.CalculateProductPrice(
            price: 100m, oldPrice: null,
            specialPrice: 70m,
            specialPriceStart: DateTimeOffset.UtcNow.AddDays(-1),
            specialPriceEnd: DateTimeOffset.UtcNow.AddDays(1));

        result.Price.Should().Be(70m);
        result.OldPrice.Should().Be(100m, "a sale with no explicit oldPrice uses the base price as reference");
        result.PercentOfSaving.Should().Be(30);
    }

    [Fact]
    public void Expired_special_price_is_ignored()
    {
        var result = _sut.CalculateProductPrice(
            price: 100m, oldPrice: null,
            specialPrice: 70m,
            specialPriceStart: DateTimeOffset.UtcNow.AddDays(-10),
            specialPriceEnd: DateTimeOffset.UtcNow.AddDays(-1));

        result.Price.Should().Be(100m);
        result.OldPrice.Should().BeNull();
        result.PercentOfSaving.Should().Be(0);
    }

    [Fact]
    public void Not_yet_started_special_price_is_ignored()
    {
        var result = _sut.CalculateProductPrice(
            price: 100m, oldPrice: null,
            specialPrice: 70m,
            specialPriceStart: DateTimeOffset.UtcNow.AddDays(1),
            specialPriceEnd: DateTimeOffset.UtcNow.AddDays(10));

        result.Price.Should().Be(100m);
    }

    [Fact]
    public void Explicit_oldPrice_is_preserved_when_higher_than_base_price()
    {
        var result = _sut.CalculateProductPrice(
            price: 100m, oldPrice: 150m,
            specialPrice: 70m,
            specialPriceStart: DateTimeOffset.UtcNow.AddDays(-1),
            specialPriceEnd: DateTimeOffset.UtcNow.AddDays(1));

        result.Price.Should().Be(70m);
        result.OldPrice.Should().Be(150m);
        result.PercentOfSaving.Should().BeInRange(53, 54); // 100 - ceil(70/150*100) = 54
    }

    [Fact]
    public void PercentOfSaving_is_zero_when_calculated_price_equals_old_price()
    {
        var result = _sut.CalculateProductPrice(price: 100m, oldPrice: 100m, specialPrice: null,
            specialPriceStart: null, specialPriceEnd: null);

        result.PercentOfSaving.Should().Be(0);
    }
}

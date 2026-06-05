using FluentAssertions;
using MockQueryable;
using Moq;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Tax.Models;
using SimplCommerce.Module.Tax.Services;
using Xunit;

namespace SimplCommerce.Module.Tax.Tests.Services;

public class TaxServiceTests
{
    private static TaxService Build(params TaxRate[] rates)
    {
        var repo = new Mock<IRepository<TaxRate>>();
        repo.Setup(x => x.Query()).Returns(rates.AsQueryable().BuildMock());
        return new TaxService(repo.Object);
    }

    [Fact]
    public async Task Returns_zero_when_tax_class_id_is_null()
    {
        var sut = Build();
        var result = await sut.GetTaxPercent(taxClassId: null, "US", 1, "10001");
        result.Should().Be(0);
    }

    [Fact]
    public async Task Returns_zero_when_no_matching_rate()
    {
        var sut = Build(new TaxRate { TaxClassId = 1, CountryId = "CA", Rate = 5m });
        var result = await sut.GetTaxPercent(1, "US", 1, "10001");
        result.Should().Be(0);
    }

    [Fact]
    public async Task Matches_on_country_when_state_null()
    {
        var sut = Build(new TaxRate
        {
            TaxClassId = 7,
            CountryId = "US",
            StateOrProvinceId = null,
            Rate = 6.25m
        });
        var result = await sut.GetTaxPercent(7, "US", 1, string.Empty);
        result.Should().Be(6.25m);
    }

    [Fact]
    public async Task Accepts_rate_matching_specific_state()
    {
        var sut = Build(
            new TaxRate { TaxClassId = 7, CountryId = "US", StateOrProvinceId = 99, Rate = 8.25m }
        );

        var result = await sut.GetTaxPercent(7, "US", 99, string.Empty);
        result.Should().Be(8.25m);
    }

    [Fact]
    public async Task Filters_by_zip_when_provided_and_match_exists()
    {
        var sut = Build(
            new TaxRate { TaxClassId = 7, CountryId = "US", Rate = 6m, ZipCode = "99999" },
            new TaxRate { TaxClassId = 7, CountryId = "US", Rate = 7m, ZipCode = "10001" }
        );
        var result = await sut.GetTaxPercent(7, "US", 1, "10001");
        result.Should().Be(7m);
    }

    [Fact]
    public async Task Matches_rate_with_empty_zipcode_when_zip_given()
    {
        var sut = Build(
            new TaxRate { TaxClassId = 7, CountryId = "US", Rate = 6m, ZipCode = null }
        );
        var result = await sut.GetTaxPercent(7, "US", 1, "10001");
        result.Should().Be(6m);
    }

    [Fact]
    public async Task Ignores_rate_with_different_tax_class()
    {
        var sut = Build(
            new TaxRate { TaxClassId = 1, CountryId = "US", Rate = 10m }
        );
        var result = await sut.GetTaxPercent(2, "US", 1, string.Empty);
        result.Should().Be(0);
    }
}

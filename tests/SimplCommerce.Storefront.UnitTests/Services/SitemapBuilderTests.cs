using System.Xml.Linq;
using Moq;
using SimplCommerce.Storefront.Services;
using SimplCommerce.Storefront.Services.ApiClients;
using Xunit;

namespace SimplCommerce.Storefront.UnitTests.Services;

public class SitemapBuilderTests
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    [Fact]
    public async Task Builds_root_categories_and_products()
    {
        var catalog = new Mock<ICatalogApi>();
        catalog.Setup(c => c.ListCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new CategoryItem(1, "Electronics", "electronics", null, 0),
                new CategoryItem(2, "Books", "books", null, 1),
            });
        catalog.Setup(c => c.ListProductsAsync(null, null, null, 1, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductListItem>(
                new[]
                {
                    new ProductListItem(10, "Laptop", "laptop", 999m, null, null, false, true, null, 0),
                    new ProductListItem(11, "C# Book", "csharp-book", 39m, null, null, false, true, null, 0),
                }, 2, 1, 200));

        var sut = new SitemapBuilder(catalog.Object);

        var xml = await sut.BuildAsync("https://shop.example.com/", CancellationToken.None);

        var doc = XDocument.Parse(xml);
        var locs = doc.Descendants(Ns + "loc").Select(e => e.Value).ToList();

        Assert.Contains("https://shop.example.com/", locs);
        Assert.Contains("https://shop.example.com/category/electronics", locs);
        Assert.Contains("https://shop.example.com/category/books", locs);
        Assert.Contains("https://shop.example.com/product/laptop", locs);
        Assert.Contains("https://shop.example.com/product/csharp-book", locs);
    }

    [Fact]
    public async Task Trims_trailing_slash_from_base_url()
    {
        var catalog = new Mock<ICatalogApi>();
        catalog.Setup(c => c.ListCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<CategoryItem>());
        catalog.Setup(c => c.ListProductsAsync(null, null, null, 1, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductListItem>(Array.Empty<ProductListItem>(), 0, 1, 200));

        var sut = new SitemapBuilder(catalog.Object);

        var xml = await sut.BuildAsync("https://shop.example.com////", CancellationToken.None);

        var doc = XDocument.Parse(xml);
        var rootLoc = doc.Descendants(Ns + "loc").First().Value;
        Assert.Equal("https://shop.example.com/", rootLoc);
    }

    [Fact]
    public async Task Stops_paginating_when_short_page_returned()
    {
        var catalog = new Mock<ICatalogApi>();
        catalog.Setup(c => c.ListCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<CategoryItem>());
        catalog.Setup(c => c.ListProductsAsync(null, null, null, 1, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductListItem>(
                new[] { new ProductListItem(1, "p", "p", 1m, null, null, false, true, null, 0) }, 1, 1, 200));

        var sut = new SitemapBuilder(catalog.Object);
        await sut.BuildAsync("https://shop.example.com", CancellationToken.None);

        catalog.Verify(c => c.ListProductsAsync(null, null, null, 2, 200, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Includes_changefreq_and_priority_for_every_url()
    {
        var catalog = new Mock<ICatalogApi>();
        catalog.Setup(c => c.ListCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new CategoryItem(1, "C", "c", null, 0) });
        catalog.Setup(c => c.ListProductsAsync(null, null, null, 1, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductListItem>(
                new[] { new ProductListItem(1, "p", "p", 1m, null, null, false, true, null, 0) }, 1, 1, 200));

        var sut = new SitemapBuilder(catalog.Object);
        var xml = await sut.BuildAsync("https://shop.example.com", CancellationToken.None);

        var doc = XDocument.Parse(xml);
        var urls = doc.Descendants(Ns + "url").ToList();
        Assert.Equal(3, urls.Count);
        Assert.All(urls, u =>
        {
            Assert.NotNull(u.Element(Ns + "changefreq"));
            Assert.NotNull(u.Element(Ns + "priority"));
        });
    }
}

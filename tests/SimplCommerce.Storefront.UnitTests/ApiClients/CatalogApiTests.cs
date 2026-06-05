using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimplCommerce.Storefront.Services.ApiClients;
using Xunit;

namespace SimplCommerce.Storefront.UnitTests.ApiClients;

public class CatalogApiTests
{
    private static (ICatalogApi sut, List<HttpRequestMessage> seen) CreateStub(
        Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        var seen = new List<HttpRequestMessage>();
        var handler = new StubHandler(req =>
        {
            seen.Add(req);
            return respond?.Invoke(req) ?? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create<object?>(null),
            };
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
        return ((ICatalogApi)new CatalogApi(http), seen);
    }

    [Fact]
    public async Task ListProductsAsync_encodes_category_brand_search_paging()
    {
        var (sut, seen) = CreateStub();
        await sut.ListProductsAsync(categoryId: 42, brandId: 7, search: "hello world",
            page: 3, pageSize: 24, ct: CancellationToken.None);

        var url = seen.Single().RequestUri!.PathAndQuery;
        url.Should().Contain("page=3");
        url.Should().Contain("pageSize=24");
        url.Should().Contain("categoryId=42");
        url.Should().Contain("brandId=7");
        url.Should().Contain("search=hello%20world");
    }

    [Fact]
    public async Task ListProductsAsync_without_filters_still_sends_paging()
    {
        var (sut, seen) = CreateStub();
        await sut.ListProductsAsync(page: 1, pageSize: 10, ct: CancellationToken.None);

        var url = seen.Single().RequestUri!.PathAndQuery;
        url.Should().StartWith("/api/storefront/catalog/products?page=1&pageSize=10");
        url.Should().NotContain("categoryId").And.NotContain("brandId").And.NotContain("search");
    }

    [Fact]
    public async Task GetProductBySlugAsync_escapes_special_chars()
    {
        var (sut, seen) = CreateStub();
        await sut.GetProductBySlugAsync("red shoes & boots", CancellationToken.None);

        seen.Single().RequestUri!.PathAndQuery
            .Should().Be("/api/storefront/catalog/products/by-slug/red%20shoes%20%26%20boots");
    }

    [Fact]
    public async Task ListCategoriesAsync_hits_the_right_path()
    {
        var (sut, seen) = CreateStub(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<CategoryItem>())
        });
        await sut.ListCategoriesAsync(CancellationToken.None);

        seen.Single().RequestUri!.PathAndQuery.Should().Be("/api/storefront/catalog/categories");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}

public class SearchApiTests
{
    [Fact]
    public async Task SearchAsync_percent_encodes_the_query()
    {
        var seen = new List<HttpRequestMessage>();
        var handler = new CapturingHandler(req =>
        {
            seen.Add(req);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SearchResponse(Array.Empty<SearchResult>(), 0, "t")),
            };
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
        var sut = new SearchApi(http);

        await sut.SearchAsync("q&p=1", 2, 30, CancellationToken.None);

        var url = seen.Single().RequestUri!.PathAndQuery;
        url.Should().Be("/api/storefront/search?q=q%26p%3D1&page=2&pageSize=30");
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}

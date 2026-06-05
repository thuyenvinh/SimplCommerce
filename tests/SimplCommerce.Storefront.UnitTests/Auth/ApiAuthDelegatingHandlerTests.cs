using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SimplCommerce.Storefront.Services.Auth;
using Xunit;

namespace SimplCommerce.Storefront.UnitTests.Auth;

public class ApiAuthDelegatingHandlerTests
{
    [Fact]
    public async Task Attaches_bearer_token_from_cookie_claim()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ApiAuthDelegatingHandler.AccessTokenClaim, "my-jwt"),
                }, authenticationType: "cookie"))
            }
        };
        var inner = new FakeInnerHandler();
        var sut = new ApiAuthDelegatingHandler(accessor) { InnerHandler = inner };
        var client = new HttpClient(sut) { BaseAddress = new Uri("https://api.test") };

        await client.GetAsync("/api/foo");

        inner.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        inner.LastRequest.Headers.Authorization.Parameter.Should().Be("my-jwt");
    }

    [Fact]
    public async Task Omits_authorization_header_when_no_claim()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
        var inner = new FakeInnerHandler();
        var sut = new ApiAuthDelegatingHandler(accessor) { InnerHandler = inner };
        var client = new HttpClient(sut) { BaseAddress = new Uri("https://api.test") };

        await client.GetAsync("/api/foo");

        inner.LastRequest!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Omits_authorization_header_when_no_httpcontext()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var inner = new FakeInnerHandler();
        var sut = new ApiAuthDelegatingHandler(accessor) { InnerHandler = inner };
        var client = new HttpClient(sut) { BaseAddress = new Uri("https://api.test") };

        await client.GetAsync("/api/foo");

        inner.LastRequest!.Headers.Authorization.Should().BeNull();
    }

    private sealed class FakeInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

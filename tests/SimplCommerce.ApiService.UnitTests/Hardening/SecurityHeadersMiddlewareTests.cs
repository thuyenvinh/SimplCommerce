using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SimplCommerce.ApiService.Hardening;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Hardening;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Sets_all_core_response_headers()
    {
        var ctx = new DefaultHttpContext();
        var sut = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await sut.InvokeAsync(ctx);

        ctx.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        ctx.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        ctx.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        ctx.Response.Headers["Permissions-Policy"].ToString().Should().Contain("geolocation=()");
    }

    [Fact]
    public async Task Csp_bans_inline_script_outside_allowed_blazor_sources()
    {
        var ctx = new DefaultHttpContext();
        var sut = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await sut.InvokeAsync(ctx);

        var csp = ctx.Response.Headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("script-src 'self' 'wasm-unsafe-eval' blob:");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().NotContain("'unsafe-inline' 'unsafe-eval'");
    }

    [Fact]
    public async Task Calls_next()
    {
        var called = false;
        var sut = new SecurityHeadersMiddleware(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(new DefaultHttpContext());

        called.Should().BeTrue();
    }
}

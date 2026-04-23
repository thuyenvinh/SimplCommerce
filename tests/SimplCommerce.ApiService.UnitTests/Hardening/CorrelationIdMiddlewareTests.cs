using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SimplCommerce.ApiService.Hardening;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Hardening;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Mints_new_id_when_client_omits_header()
    {
        var (ctx, sut) = Setup();
        await sut.InvokeAsync(ctx);

        ctx.TraceIdentifier.Should().NotBeNullOrEmpty();
        ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
            .Should().NotBeNullOrEmpty().And.Be(ctx.TraceIdentifier);
    }

    [Fact]
    public async Task Honors_client_supplied_header()
    {
        var (ctx, sut) = Setup();
        ctx.Request.Headers[CorrelationIdMiddleware.HeaderName] = "caller-provided-id";

        await sut.InvokeAsync(ctx);

        ctx.TraceIdentifier.Should().Be("caller-provided-id");
        ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
            .Should().Be("caller-provided-id");
    }

    [Fact]
    public async Task Blank_client_header_is_ignored_and_id_is_minted()
    {
        var (ctx, sut) = Setup();
        ctx.Request.Headers[CorrelationIdMiddleware.HeaderName] = "";

        await sut.InvokeAsync(ctx);

        ctx.TraceIdentifier.Should().NotBeNullOrEmpty().And.NotBe("");
    }

    [Fact]
    public async Task Next_is_invoked_exactly_once()
    {
        var calls = 0;
        RequestDelegate next = _ => { calls++; return Task.CompletedTask; };

        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var ctx = new DefaultHttpContext { RequestServices = services };
        var sut = new CorrelationIdMiddleware(next);
        await sut.InvokeAsync(ctx);

        calls.Should().Be(1);
    }

    private static (DefaultHttpContext ctx, CorrelationIdMiddleware sut) Setup()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var services = new ServiceCollection()
            .AddSingleton(NullLoggerFactory.Instance)
            .AddLogging()
            .BuildServiceProvider();

        var ctx = new DefaultHttpContext { RequestServices = services };
        var sut = new CorrelationIdMiddleware(next);
        return (ctx, sut);
    }
}

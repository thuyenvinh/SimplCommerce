using FluentAssertions;
using Ganss.Xss;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.ApiService.Hardening;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Hardening;

public class HtmlSanitizerExtensionsTests
{
    private static IHtmlSanitizer CreateSut()
    {
        var services = new ServiceCollection().AddSimplHtmlSanitizer().BuildServiceProvider();
        return services.GetRequiredService<IHtmlSanitizer>();
    }

    [Fact]
    public void Singleton_is_registered()
    {
        var services = new ServiceCollection().AddSimplHtmlSanitizer().BuildServiceProvider();
        services.GetRequiredService<IHtmlSanitizer>()
            .Should().BeSameAs(services.GetRequiredService<IHtmlSanitizer>());
    }

    [Fact]
    public void Strips_script_tags()
    {
        var sut = CreateSut();
        var dirty = "<p>hello</p><script>alert(1)</script>";
        sut.Sanitize(dirty).Should().NotContain("<script>").And.Contain("<p>hello</p>");
    }

    [Theory]
    [InlineData("iframe")]
    [InlineData("object")]
    [InlineData("embed")]
    [InlineData("form")]
    public void Strips_dangerous_container_tags(string tag)
    {
        var sut = CreateSut();
        var dirty = $"<{tag}>payload</{tag}><p>clean</p>";
        sut.Sanitize(dirty).Should().NotContain($"<{tag}").And.Contain("<p>clean</p>");
    }

    [Fact]
    public void Strips_javascript_href()
    {
        var sut = CreateSut();
        var dirty = "<a href=\"javascript:alert(1)\">click</a>";
        sut.Sanitize(dirty).Should().NotContain("javascript:");
    }

    [Fact]
    public void Preserves_safe_markup()
    {
        var sut = CreateSut();
        var clean = "<p>hello <strong>world</strong></p><a href=\"https://ok.example\">ok</a>";
        sut.Sanitize(clean).Should().Contain("<strong>world</strong>")
            .And.Contain("https://ok.example");
    }
}

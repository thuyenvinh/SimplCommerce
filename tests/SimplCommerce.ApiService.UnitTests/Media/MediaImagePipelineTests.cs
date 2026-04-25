using SimplCommerce.ApiService.Media;
using SixLabors.ImageSharp.Web.Commands;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Media;

public class MediaImagePipelineTests
{
    [Fact]
    public void Clamps_width_above_maximum()
    {
        var commands = new CommandCollection { { "width", "99999" } };

        MediaImagePipeline.SanitizeCommands(commands);

        Assert.Equal("4000", commands["width"]);
    }

    [Fact]
    public void Clamps_width_below_minimum()
    {
        var commands = new CommandCollection { { "width", "0" } };

        MediaImagePipeline.SanitizeCommands(commands);

        Assert.Equal("1", commands["width"]);
    }

    [Fact]
    public void Clamps_quality_into_band()
    {
        var commands = new CommandCollection { { "quality", "100" }, { "height", "5000" } };

        MediaImagePipeline.SanitizeCommands(commands);

        Assert.Equal("90", commands["quality"]);
        Assert.Equal("4000", commands["height"]);
    }

    [Fact]
    public void Drops_unknown_commands()
    {
        var commands = new CommandCollection
        {
            { "width", "300" },
            { "watermark", "malicious" },
            { "process", "evil" }
        };

        MediaImagePipeline.SanitizeCommands(commands);

        Assert.True(commands.TryGetValue("width", out _));
        Assert.False(commands.TryGetValue("watermark", out _));
        Assert.False(commands.TryGetValue("process", out _));
    }

    [Fact]
    public void Keeps_full_whitelist()
    {
        var commands = new CommandCollection
        {
            { "width", "300" },
            { "height", "200" },
            { "rmode", "crop" },
            { "format", "webp" },
            { "quality", "80" }
        };

        MediaImagePipeline.SanitizeCommands(commands);

        Assert.Equal(5, commands.Count);
    }

    [Fact]
    public void Ignores_non_numeric_values()
    {
        var commands = new CommandCollection { { "width", "not-a-number" } };

        MediaImagePipeline.SanitizeCommands(commands);

        Assert.Equal("not-a-number", commands["width"]);
    }
}

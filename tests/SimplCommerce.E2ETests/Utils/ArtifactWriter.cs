using Microsoft.Playwright;

namespace SimplCommerce.E2ETests.Utils;

/// <summary>
/// Centralizes artifact paths so screenshots / videos / traces all live under
/// a single layout that's easy to mirror into the user-documentation site:
///
///   Artifacts/Screenshots/{Module}/{TestName}/{NN}-{Step}.png
///   Artifacts/Videos/{Module}/{TestName}/{file}.webm
///   Artifacts/Traces/{Module}/{TestName}/trace.zip
///
/// Step counter auto-increments so authors don't have to renumber when they
/// add a step mid-flow.
/// </summary>
public sealed class ArtifactWriter
{
    private readonly string _root;
    private readonly string _module;
    private readonly string _testName;
    private int _step;

    public ArtifactWriter(string moduleName, string testName)
    {
        _module = Sanitize(moduleName);
        _testName = Sanitize(testName);
        // Walk up from BaseDirectory (bin/Debug/net9.0/) to the test project root
        // so artifacts land alongside the source tree, not inside bin.
        var probe = new DirectoryInfo(AppContext.BaseDirectory);
        while (probe is not null && !File.Exists(Path.Combine(probe.FullName, "SimplCommerce.E2ETests.csproj")))
        {
            probe = probe.Parent;
        }
        _root = probe?.FullName ?? AppContext.BaseDirectory;
    }

    public string ScreenshotDir => EnsureDir(Path.Combine(_root, "Artifacts", "Screenshots", _module, _testName));
    public string VideoDir => EnsureDir(Path.Combine(_root, "Artifacts", "Videos", _module, _testName));
    public string TraceDir => EnsureDir(Path.Combine(_root, "Artifacts", "Traces", _module, _testName));

    /// <summary>
    /// Capture a documentation-quality full-page screenshot tagged with the
    /// given step name. Step numbers auto-increment so adding a step mid-flow
    /// doesn't force a manual renumber.
    /// </summary>
    public async Task<string> CaptureStepAsync(IPage page, string stepName, bool fullPage = true)
    {
        _step++;
        var fileName = $"{_step:D2}-{Sanitize(stepName)}.png";
        var path = Path.Combine(ScreenshotDir, fileName);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = fullPage,
            Animations = ScreenshotAnimations.Disabled,
        });
        return path;
    }

    /// <summary>
    /// Failure-mode screenshot — separate counter so it doesn't pollute the
    /// "happy path" numbering used for docs.
    /// </summary>
    public async Task<string> CaptureFailureAsync(IPage page, string label = "failure")
    {
        var fileName = $"FAILURE-{Sanitize(label)}-{DateTime.UtcNow:yyyyMMddHHmmss}.png";
        var path = Path.Combine(ScreenshotDir, fileName);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true,
        });
        return path;
    }

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Sanitize(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var c in input)
        {
            sb.Append(invalid.Contains(c) || c == ' ' ? '-' : c);
        }
        return sb.ToString().Trim('-');
    }
}

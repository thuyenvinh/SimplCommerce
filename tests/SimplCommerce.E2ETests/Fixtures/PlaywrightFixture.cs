using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using SimplCommerce.E2ETests.Utils;

namespace SimplCommerce.E2ETests.Fixtures;

/// <summary>
/// Base for every E2E test class. Handles Playwright init, browser launch,
/// per-test context with video / trace, and artifact organization.
///
/// Convention: subclasses set <see cref="Module"/> in their constructor or
/// override the property — it controls the Artifacts/{Module}/ subfolder.
/// </summary>
public abstract class PlaywrightFixture
{
    private static IPlaywright? s_playwright;
    private static IBrowser? s_browser;
    protected static E2EConfig Config { get; private set; } = null!;

    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;
    protected ArtifactWriter Artifacts { get; private set; } = null!;

    protected virtual string Module => GetType().Namespace?.Split('.').LastOrDefault() ?? "General";

    [OneTimeSetUp]
    public async Task GlobalSetUp()
    {
        Config = E2EConfig.Load();
        s_playwright ??= await Playwright.CreateAsync();
        s_browser ??= await s_playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Config.Headless,
            SlowMo = Config.SlowMoMs,
        });
    }

    [SetUp]
    public async Task PerTestSetUp()
    {
        var testName = TestContext.CurrentContext.Test.MethodName ?? "unnamed";
        Artifacts = new ArtifactWriter(Module, testName);

        var contextOptions = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Config.ViewportWidth, Height = Config.ViewportHeight },
            IgnoreHTTPSErrors = true,
            BaseURL = Config.AdminBaseUrl,
        };
        if (!string.Equals(Config.VideoMode, "Off", StringComparison.OrdinalIgnoreCase))
        {
            contextOptions.RecordVideoDir = Artifacts.VideoDir;
            contextOptions.RecordVideoSize = new RecordVideoSize { Width = Config.ViewportWidth, Height = Config.ViewportHeight };
        }
        Context = await s_browser!.NewContextAsync(contextOptions);
        Context.SetDefaultTimeout(Config.ActionTimeoutMs);
        Context.SetDefaultNavigationTimeout(Config.NavigationTimeoutMs);

        if (!string.Equals(Config.TraceMode, "Off", StringComparison.OrdinalIgnoreCase))
        {
            await Context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            });
        }

        Page = await Context.NewPageAsync();
    }

    [TearDown]
    public async Task PerTestTearDown()
    {
        var status = TestContext.CurrentContext.Result.Outcome.Status;
        var failed = status == TestStatus.Failed || status == TestStatus.Inconclusive;

        // Trace policy.
        var keepTrace = string.Equals(Config.TraceMode, "On", StringComparison.OrdinalIgnoreCase)
            || (failed && string.Equals(Config.TraceMode, "OnFailure", StringComparison.OrdinalIgnoreCase));
        if (!string.Equals(Config.TraceMode, "Off", StringComparison.OrdinalIgnoreCase))
        {
            var tracePath = keepTrace
                ? Path.Combine(Artifacts.TraceDir, "trace.zip")
                : null;
            await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
        }

        // Capture failure screenshot before closing the context so video keeps
        // recording the failure frame.
        if (failed)
        {
            try { await Artifacts.CaptureFailureAsync(Page); }
            catch { /* page may be closed already; trace + video remain */ }
        }

        await Context.CloseAsync(); // flushes video

        // Drop the video file if the policy says we don't keep it (success +
        // OnFailure mode). Playwright already wrote it; we delete after the
        // close so the file handle is released.
        if (!failed && string.Equals(Config.VideoMode, "OnFailure", StringComparison.OrdinalIgnoreCase))
        {
            try { Directory.Delete(Artifacts.VideoDir, recursive: true); }
            catch { /* best effort */ }
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        if (s_browser is not null) await s_browser.CloseAsync();
        s_playwright?.Dispose();
        s_browser = null;
        s_playwright = null;
    }
}

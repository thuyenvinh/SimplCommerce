using Microsoft.Extensions.Configuration;

namespace SimplCommerce.E2ETests.Utils;

/// <summary>
/// Reads E2E settings from (in order of precedence):
///   1. environment variables prefixed E2E__  (e.g. E2E__AdminBaseUrl)
///   2. appsettings.Test.json (defaults — never commit real credentials)
///
/// Credentials live in env vars in CI / dev so secrets stay out of source
/// control. The defaults in appsettings.Test.json are the SimplCommerce
/// seed-data admin account that any fresh database ships with.
/// </summary>
public sealed class E2EConfig
{
    public string AdminBaseUrl { get; init; } = "http://localhost:5236";
    public string StorefrontBaseUrl { get; init; } = "http://localhost:5237";
    public string ApiBaseUrl { get; init; } = "http://localhost:5096";
    public string AdminEmail { get; init; } = string.Empty;
    public string AdminPassword { get; init; } = string.Empty;
    public bool Headless { get; init; } = true;
    public int SlowMoMs { get; init; } = 0;
    public int ViewportWidth { get; init; } = 1440;
    public int ViewportHeight { get; init; } = 900;
    public string VideoMode { get; init; } = "On";          // On / OnFailure / Off
    public string ScreenshotMode { get; init; } = "On";     // On / OnFailure / Off
    public string TraceMode { get; init; } = "OnFailure";   // On / OnFailure / Off
    public int ActionTimeoutMs { get; init; } = 15000;
    public int NavigationTimeoutMs { get; init; } = 30000;

    public static E2EConfig Load()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "E2E__")
            .AddEnvironmentVariables();
        var root = builder.Build();
        var cfg = new E2EConfig();
        root.GetSection("E2E").Bind(cfg);
        return cfg;
    }
}

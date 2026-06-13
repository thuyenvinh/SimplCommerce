using NUnit.Framework;
using SimplCommerce.E2ETests.Pages;

namespace SimplCommerce.E2ETests.Fixtures;

/// <summary>
/// Extends PlaywrightFixture by signing the test in as the seed admin before
/// each test runs. Most CRUD + navigation suites inherit from this; the auth
/// suite itself inherits from PlaywrightFixture directly.
/// </summary>
public abstract class AuthenticatedFixture : PlaywrightFixture
{
    [SetUp]
    public async Task SignInAsAdmin()
    {
        if (string.IsNullOrWhiteSpace(Config.AdminEmail) || string.IsNullOrWhiteSpace(Config.AdminPassword))
        {
            Assert.Inconclusive("E2E__AdminEmail / E2E__AdminPassword not configured — set them via env var or appsettings.Test.json.");
        }
        var login = new LoginPage(Page);
        await login.GoToAsync();
        await login.LoginAsync(Config.AdminEmail, Config.AdminPassword);
        await Page.WaitForURLAsync(u => !u.Contains("/login"), new() { Timeout = Config.NavigationTimeoutMs });
    }
}

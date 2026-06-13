using Microsoft.Playwright;
using NUnit.Framework;
using SimplCommerce.E2ETests.Fixtures;
using SimplCommerce.E2ETests.Pages;

namespace SimplCommerce.E2ETests.Tests.Auth;

[TestFixture]
[NonParallelizable]
public class AuthTests : PlaywrightFixture
{
    protected override string Module => "Auth";

    [Test, Order(1)]
    public async Task Login_page_renders_email_and_password_fields()
    {
        var login = new LoginPage(Page);
        await login.GoToAsync();
        await Artifacts.CaptureStepAsync(Page, "login-page-loaded");
        await Assertions.Expect(login.EmailField).ToBeVisibleAsync();
        await Assertions.Expect(login.PasswordField).ToBeVisibleAsync();
        await Assertions.Expect(login.SignInButton).ToBeVisibleAsync();
    }

    [Test, Order(2)]
    public async Task Login_with_wrong_password_shows_error_alert()
    {
        var login = new LoginPage(Page);
        await login.GoToAsync();
        await login.LoginAsync(Config.AdminEmail, "definitely-not-the-password");
        await Assertions.Expect(login.ErrorAlert).ToBeVisibleAsync(new() { Timeout = Config.ActionTimeoutMs });
        await Artifacts.CaptureStepAsync(Page, "login-failed-error-shown");
    }

    [Test, Order(3)]
    public async Task Login_with_seed_admin_credentials_lands_on_dashboard()
    {
        var login = new LoginPage(Page);
        await login.GoToAsync();
        await login.LoginAsync(Config.AdminEmail, Config.AdminPassword);
        await Page.WaitForURLAsync(u => !u.Contains("/login"), new() { Timeout = Config.NavigationTimeoutMs });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var dashboard = new DashboardPage(Page);
        await Assertions.Expect(dashboard.Heading).ToBeVisibleAsync(new() { Timeout = Config.NavigationTimeoutMs });
        await Artifacts.CaptureStepAsync(Page, "dashboard-after-login");
    }

    [Test, Order(4)]
    public async Task Anonymous_visit_to_admin_redirects_to_login()
    {
        await Page.GotoAsync("/products");
        await Page.WaitForURLAsync(u => u.Contains("/login"));
        await Artifacts.CaptureStepAsync(Page, "redirected-to-login");
    }

    [Test, Order(5)]
    public async Task Logout_returns_user_to_login()
    {
        // Re-auth for this isolated test, then sign out.
        var login = new LoginPage(Page);
        await login.GoToAsync();
        await login.LoginAsync(Config.AdminEmail, Config.AdminPassword);
        await Page.WaitForURLAsync(u => !u.Contains("/login"));
        await Page.GotoAsync("/logout");
        await Page.WaitForURLAsync(u => u.Contains("/login"));
        await Artifacts.CaptureStepAsync(Page, "logged-out");
    }
}

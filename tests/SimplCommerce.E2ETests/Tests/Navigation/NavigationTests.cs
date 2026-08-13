using Microsoft.Playwright;
using NUnit.Framework;
using SimplCommerce.E2ETests.Fixtures;
using SimplCommerce.E2ETests.Pages;

namespace SimplCommerce.E2ETests.Tests.Navigation;

[TestFixture]
public class NavigationTests : AuthenticatedFixture
{
    protected override string Module => "Navigation";

    [Test]
    public async Task Dashboard_loads_with_KPI_strip()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.GoToAsync();
        await Assertions.Expect(dashboard.Heading).ToBeVisibleAsync();
        await Artifacts.CaptureStepAsync(Page, "dashboard");
    }

    [TestCase("Products", "/products")]
    [TestCase("Categories", "/categories")]
    [TestCase("Brands", "/brands")]
    [TestCase("Orders", "/orders")]
    [TestCase("Sales report", "/sales-report")]
    [TestCase("Users", "/users")]
    [TestCase("Reviews", "/reviews")]
    [TestCase("Comments", "/comments")]
    [TestCase("Vendors", "/vendors")]
    public async Task Main_nav_link_opens_its_page(string label, string expectedPath)
    {
        // Use direct URL navigation rather than clicking the nav link: several
        // groups in the MainLayout drawer (Sales, Customers, Content, etc.) start
        // collapsed, and expanding each one before the click adds 9 fragile
        // ClickAsync calls per test case. The user's contract — "/{path} loads
        // its admin page" — is what we're verifying; the click is incidental.
        var dashboard = new DashboardPage(Page);
        await dashboard.GoToAsync();
        await Page.GotoAsync(expectedPath);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForURLAsync(u => u.Contains(expectedPath), new() { Timeout = Config.NavigationTimeoutMs });
        await Artifacts.CaptureStepAsync(Page, $"nav-{label.ToLowerInvariant().Replace(' ', '-')}");
    }
}

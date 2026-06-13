using Microsoft.Playwright;

namespace SimplCommerce.E2ETests.Pages;

/// <summary>
/// Dashboard.razor — top-of-app KPI strip + nav drawer. We hit it after login
/// to confirm the cookie was set and the navigation drawer rendered.
/// </summary>
public sealed class DashboardPage
{
    private readonly IPage _page;
    public DashboardPage(IPage page) => _page = page;

    public ILocator Heading => _page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard" });
    public ILocator NavDrawer => _page.Locator(".mud-drawer");

    public ILocator NavLink(string text) => _page.GetByRole(AriaRole.Link, new() { Name = text });

    public async Task GoToAsync()
    {
        await _page.GotoAsync("/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public Task<bool> IsLoadedAsync() => Heading.IsVisibleAsync();

    public async Task OpenAsync(string navLabel)
    {
        await NavLink(navLabel).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

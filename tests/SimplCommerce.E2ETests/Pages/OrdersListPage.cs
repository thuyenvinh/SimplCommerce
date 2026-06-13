using Microsoft.Playwright;

namespace SimplCommerce.E2ETests.Pages;

/// <summary>
/// OrdersList.razor — paginated MudTable of orders with status filter +
/// customer search.
/// </summary>
public sealed class OrdersListPage
{
    private readonly IPage _page;
    public OrdersListPage(IPage page) => _page = page;

    public ILocator Heading => _page.GetByRole(AriaRole.Heading, new() { Name = "Orders" });
    public ILocator StatusFilter => _page.GetByLabel("Status");
    public ILocator CustomerSearchField => _page.GetByLabel("Search customer");
    public ILocator Table => _page.Locator("table.mud-table-root");

    public ILocator RowById(long id) => _page.Locator($"tr:has(td:has-text('{id}'))").First;

    public async Task GoToAsync()
    {
        await _page.GotoAsync("/orders");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SearchCustomerAsync(string term)
    {
        await CustomerSearchField.FillAsync(term);
        await CustomerSearchField.PressAsync("Enter");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

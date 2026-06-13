using Microsoft.Playwright;

namespace SimplCommerce.E2ETests.Pages;

/// <summary>
/// ProductsList.razor — paginated MudTable of products with a search box +
/// New product button.
/// </summary>
public sealed class ProductsListPage
{
    private readonly IPage _page;
    public ProductsListPage(IPage page) => _page = page;

    public ILocator Heading => _page.GetByRole(AriaRole.Heading, new() { Name = "Products" });
    public ILocator SearchField => _page.GetByLabel("Search", new() { Exact = false });
    public ILocator NewProductButton => _page.GetByRole(AriaRole.Button, new() { Name = "New product" });
    public ILocator Table => _page.Locator("table.mud-table-root");

    public ILocator RowByName(string name) => _page.GetByRole(AriaRole.Row, new() { Name = name });

    public async Task GoToAsync()
    {
        await _page.GotoAsync("/products");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SearchAsync(string term)
    {
        await SearchField.FillAsync(term);
        // ProductsList uses ValueChanged debounce; press Enter to force.
        await SearchField.PressAsync("Enter");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task OpenNewAsync()
    {
        await NewProductButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task OpenAsync(string productName)
    {
        await RowByName(productName).First.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

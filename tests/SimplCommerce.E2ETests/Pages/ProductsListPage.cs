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
    public ILocator SearchField => _page.GetByLabel("Search by name / SKU");
    // MudButton with Href renders as <a> not <button> → AriaRole.Link, not Button.
    public ILocator NewProductButton => _page.GetByRole(AriaRole.Link, new() { Name = "New product" });
    public ILocator Table => _page.Locator("table.mud-table-root");

    // GetByRole(Row, Name=...) matches accessibility name which is the row's
    // full text. Plain text-content match on the Name column is more reliable
    // because the row also contains ID + price columns that change between runs.
    public ILocator RowByName(string name) => _page.Locator("tbody tr").Filter(new() { HasText = name });

    public async Task GoToAsync()
    {
        await _page.GotoAsync("/products");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SearchAsync(string term)
    {
        await SearchField.FillAsync(term);
        // MudTextField uses Immediate=false + 400 ms debounce on
        // OnDebounceIntervalElapsed; Enter doesn't bypass it. Wait the debounce
        // window out, then for the network call to settle.
        await _page.WaitForTimeoutAsync(500);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task OpenNewAsync()
    {
        await NewProductButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task OpenAsync(string productName)
    {
        // The MudTable row has no whole-row click — Edit lives behind the
        // pencil MudIconButton (aria-label="Edit") in the Actions column.
        await RowByName(productName).GetByLabel("Edit").First.ClickAsync();
        await _page.WaitForURLAsync(u => u.Contains("/products/edit/"));
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

using Microsoft.Playwright;

namespace SimplCommerce.E2ETests.Pages;

/// <summary>
/// ProductEdit.razor — used for both create (/products/create) and edit
/// (/products/edit/{id}). All input fields are MudTextField/MudNumericField
/// bound by Label so GetByLabel is the stable selector.
/// </summary>
public sealed class ProductEditPage
{
    private readonly IPage _page;
    public ProductEditPage(IPage page) => _page = page;

    public ILocator NameField => _page.GetByLabel("Name", new() { Exact = true });
    public ILocator SlugField => _page.GetByLabel("Slug");
    public ILocator SkuField => _page.GetByLabel("SKU");
    public ILocator ShortDescriptionField => _page.GetByLabel("Short description");
    public ILocator DescriptionField => _page.GetByLabel("Description", new() { Exact = true });
    public ILocator PriceField => _page.GetByLabel("Price", new() { Exact = true });
    public ILocator PublishedToggle => _page.GetByLabel("Published");
    public ILocator AllowToOrderToggle => _page.GetByLabel("Allow to order");

    public ILocator SaveButton => _page.GetByRole(AriaRole.Button, new() { Name = "Save" });
    public ILocator DeleteButton => _page.GetByRole(AriaRole.Button, new() { Name = "Delete" });
    public ILocator SuccessToast => _page.Locator(".mud-snackbar.mud-alert-filled-success");
    public ILocator ErrorToast => _page.Locator(".mud-snackbar.mud-alert-filled-error");
    public ILocator ValidationMessage(string fieldLabel) =>
        _page.Locator($"label:has-text('{fieldLabel}')")
             .Locator("xpath=ancestor::div[contains(@class,'mud-input-control')]")
             .Locator(".mud-input-helper-text.mud-input-error");

    public async Task GoToCreateAsync()
    {
        await _page.GotoAsync("/products/create");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task FillRequiredAsync(string name, string slug, decimal price)
    {
        await NameField.FillAsync(name);
        await SlugField.FillAsync(slug);
        await PriceField.FillAsync(price.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public async Task SaveAsync()
    {
        await SaveButton.ClickAsync();
    }

    public Task<bool> SavedSuccessfullyAsync() => SuccessToast.IsVisibleAsync();
}

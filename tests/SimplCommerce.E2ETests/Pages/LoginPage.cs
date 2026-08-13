using Microsoft.Playwright;

namespace SimplCommerce.E2ETests.Pages;

/// <summary>
/// Wraps the admin sign-in page (Login.razor). Selectors derived from MudBlazor
/// rendering — the EditForm uses Required="true" + MudTextField Label, so
/// Playwright's GetByLabel hits the rendered &lt;label&gt; reliably.
/// </summary>
public sealed class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page) => _page = page;

    public ILocator EmailField => _page.GetByLabel("Email", new() { Exact = false });
    public ILocator PasswordField => _page.GetByLabel("Password", new() { Exact = false });
    public ILocator SignInButton => _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
    public ILocator ErrorAlert => _page.GetByRole(AriaRole.Alert);

    public async Task GoToAsync(string? returnUrl = null)
    {
        var url = returnUrl is null ? "/login" : $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
        await _page.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task LoginAsync(string email, string password)
    {
        await EmailField.FillAsync(email);
        await PasswordField.FillAsync(password);
        await SignInButton.ClickAsync();
    }

    public Task<bool> IsErrorShownAsync() => ErrorAlert.IsVisibleAsync();
}

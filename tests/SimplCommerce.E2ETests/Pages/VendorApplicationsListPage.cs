using Microsoft.Playwright;

namespace SimplCommerce.E2ETests.Pages;

/// <summary>
/// VendorApplicationsList.razor (Wave 7) — admin onboarding queue.
/// </summary>
public sealed class VendorApplicationsListPage
{
    private readonly IPage _page;
    public VendorApplicationsListPage(IPage page) => _page = page;

    public ILocator Heading => _page.GetByRole(AriaRole.Heading, new() { Name = "Vendor applications" });
    public ILocator StatusFilter => _page.GetByLabel("Status");
    public ILocator ApproveButton(long applicationId) =>
        _page.Locator($"tr:has(td:text-is('{applicationId}'))").GetByRole(AriaRole.Button, new() { Name = "Approve" });
    public ILocator RejectButton(long applicationId) =>
        _page.Locator($"tr:has(td:text-is('{applicationId}'))").GetByRole(AriaRole.Button, new() { Name = "Reject" });
    public ILocator DecisionDialog => _page.GetByRole(AriaRole.Dialog);
    public ILocator DecisionNote => DecisionDialog.GetByLabel("Decision note", new() { Exact = false });

    public async Task GoToAsync()
    {
        await _page.GotoAsync("/vendors/applications");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

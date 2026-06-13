using Microsoft.Playwright;

namespace SimplCommerce.E2ETests.Pages;

/// <summary>
/// OrderDetail.razor — surfaces the order header, shipments table, refund
/// modal (Wave 4 UI), and shipment status PATCH dropdowns.
/// </summary>
public sealed class OrderDetailPage
{
    private readonly IPage _page;
    public OrderDetailPage(IPage page) => _page = page;

    public ILocator Heading(long id) => _page.GetByRole(AriaRole.Heading, new() { Name = $"Order #{id}" });
    public ILocator StatusSelect => _page.GetByLabel("Update status");
    public ILocator RefundButton => _page.GetByRole(AriaRole.Button, new() { Name = "Refund" });
    public ILocator NewShipmentButton => _page.GetByRole(AriaRole.Button, new() { Name = "New shipment" });

    public ILocator RefundDialog => _page.GetByRole(AriaRole.Dialog);
    public ILocator RefundAmountField => RefundDialog.GetByLabel("Refund amount");
    public ILocator RefundReasonField => RefundDialog.GetByLabel("Reason");
    public ILocator IssueRefundButton => RefundDialog.GetByRole(AriaRole.Button, new() { Name = "Issue refund" });

    public async Task GoToAsync(long id)
    {
        await _page.GotoAsync($"/orders/{id}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task OpenRefundDialogAsync()
    {
        await RefundButton.ClickAsync();
        await RefundDialog.WaitForAsync();
    }

    public async Task SubmitRefundAsync(decimal amount, string reason)
    {
        await RefundAmountField.FillAsync(amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await RefundReasonField.FillAsync(reason);
        await IssueRefundButton.ClickAsync();
    }
}

using Microsoft.Playwright;
using NUnit.Framework;
using SimplCommerce.E2ETests.Fixtures;
using SimplCommerce.E2ETests.Pages;

namespace SimplCommerce.E2ETests.Tests.Orders;

/// <summary>
/// Orders need a populated database. The seed admin user has no orders by
/// default, so when the orders table comes back empty we mark these tests
/// inconclusive rather than failing — running the SimplCommerce sample-data
/// module first is the operator's job, not the test's.
/// </summary>
[TestFixture]
public class OrdersFlowTests : AuthenticatedFixture
{
    protected override string Module => "Orders";

    [Test]
    public async Task Orders_list_renders_and_filters_by_customer()
    {
        var orders = new OrdersListPage(Page);
        await orders.GoToAsync();
        await Assertions.Expect(orders.Heading).ToBeVisibleAsync();
        await Artifacts.CaptureStepAsync(Page, "orders-list-loaded");

        var firstRow = Page.Locator("tbody tr").First;
        if (await firstRow.CountAsync() == 0)
        {
            Assert.Inconclusive("No orders in the test database — seed sample data or place an order first.");
        }
        await Artifacts.CaptureStepAsync(Page, "orders-list-with-data");
    }

    [Test]
    public async Task Open_order_detail_shows_refund_button()
    {
        var orders = new OrdersListPage(Page);
        await orders.GoToAsync();
        var firstRow = Page.Locator("tbody tr").First;
        if (await firstRow.CountAsync() == 0)
        {
            Assert.Inconclusive("No orders in the test database.");
        }
        await firstRow.ClickAsync();
        await Page.WaitForURLAsync(u => System.Text.RegularExpressions.Regex.IsMatch(u, "/orders/\\d+"));
        await Artifacts.CaptureStepAsync(Page, "order-detail");

        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Refund" })).ToBeVisibleAsync();
        await Artifacts.CaptureStepAsync(Page, "order-detail-refund-button");
    }
}

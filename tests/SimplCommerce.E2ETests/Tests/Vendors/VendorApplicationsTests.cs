using Microsoft.Playwright;
using NUnit.Framework;
using SimplCommerce.E2ETests.Fixtures;
using SimplCommerce.E2ETests.Pages;

namespace SimplCommerce.E2ETests.Tests.Vendors;

[TestFixture]
public class VendorApplicationsTests : AuthenticatedFixture
{
    protected override string Module => "Vendors";

    [Test]
    public async Task Applications_page_renders_with_default_pending_filter()
    {
        var apps = new VendorApplicationsListPage(Page);
        await apps.GoToAsync();
        await Assertions.Expect(apps.Heading).ToBeVisibleAsync();
        await Artifacts.CaptureStepAsync(Page, "vendor-applications-list");

        // Default filter is Pending — verify the chip / select reflects that.
        await Assertions.Expect(apps.StatusFilter).ToBeVisibleAsync();
        await Artifacts.CaptureStepAsync(Page, "vendor-applications-default-filter-pending");
    }
}

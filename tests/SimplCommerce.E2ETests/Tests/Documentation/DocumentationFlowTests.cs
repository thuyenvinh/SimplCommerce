using Microsoft.Playwright;
using NUnit.Framework;
using SimplCommerce.E2ETests.Fixtures;
using SimplCommerce.E2ETests.Pages;
using SimplCommerce.E2ETests.TestData;

namespace SimplCommerce.E2ETests.Tests.Documentation;

/// <summary>
/// Dedicated "happy path" walkthroughs whose screenshots + videos feed the
/// end-user docs. Each test corresponds to one chapter / how-to article and
/// runs at a slower pace so the captures show the UI at rest, not mid-render.
/// </summary>
[TestFixture]
[NonParallelizable]
public class DocumentationFlowTests : AuthenticatedFixture
{
    protected override string Module => "Documentation";

    [Test]
    public async Task Walkthrough_signin_and_dashboard_overview()
    {
        // This re-signs in inside the test (rather than relying on the inherited
        // setup) so the screenshot sequence covers the login page itself for
        // the "Signing in" article.
        await Page.GotoAsync("/logout");
        await Page.WaitForURLAsync(u => u.Contains("/login"));
        await Artifacts.CaptureStepAsync(Page, "step-1-login-screen");

        var login = new LoginPage(Page);
        await login.EmailField.FillAsync(Config.AdminEmail);
        await Artifacts.CaptureStepAsync(Page, "step-2-email-entered");
        await login.PasswordField.FillAsync(Config.AdminPassword);
        await Artifacts.CaptureStepAsync(Page, "step-3-password-entered");
        await login.SignInButton.ClickAsync();
        await Page.WaitForURLAsync(u => !u.Contains("/login"));
        await Artifacts.CaptureStepAsync(Page, "step-4-dashboard");
    }

    [Test]
    public async Task Walkthrough_create_first_product()
    {
        var (name, slug, price) = TestDataFactory.NewProduct("docs");

        var list = new ProductsListPage(Page);
        await list.GoToAsync();
        await Artifacts.CaptureStepAsync(Page, "step-1-open-products");

        await list.OpenNewAsync();
        await Artifacts.CaptureStepAsync(Page, "step-2-new-product-form");

        var edit = new ProductEditPage(Page);
        await edit.NameField.FillAsync(name);
        await edit.SlugField.FillAsync(slug);
        await Artifacts.CaptureStepAsync(Page, "step-3-fill-general");

        await edit.PriceField.FillAsync(price.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await Artifacts.CaptureStepAsync(Page, "step-4-fill-price");

        await edit.SaveAsync();
        await Page.WaitForURLAsync(u => u.Contains("/products") && !u.Contains("/create"));
        await Artifacts.CaptureStepAsync(Page, "step-5-back-on-list");
    }
}

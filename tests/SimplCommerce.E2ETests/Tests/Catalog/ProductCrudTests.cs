using Microsoft.Playwright;
using NUnit.Framework;
using SimplCommerce.E2ETests.Fixtures;
using SimplCommerce.E2ETests.Pages;
using SimplCommerce.E2ETests.TestData;

namespace SimplCommerce.E2ETests.Tests.Catalog;

[TestFixture]
public class ProductCrudTests : AuthenticatedFixture
{
    protected override string Module => "Catalog";

    [Test]
    public async Task Create_product_then_find_it_in_search()
    {
        var (name, slug, price) = TestDataFactory.NewProduct("create");

        // Step 1: navigate to products list
        var list = new ProductsListPage(Page);
        await list.GoToAsync();
        await Artifacts.CaptureStepAsync(Page, "products-list");

        // Step 2: open New product form
        await list.OpenNewAsync();
        await Artifacts.CaptureStepAsync(Page, "new-product-form-empty");

        // Step 3: fill in required fields + save
        var edit = new ProductEditPage(Page);
        await edit.FillRequiredAsync(name, slug, price);
        await Artifacts.CaptureStepAsync(Page, "new-product-form-filled");
        await edit.SaveAsync();
        await Page.WaitForURLAsync(u => u.Contains("/products") && !u.Contains("/create"));
        await Artifacts.CaptureStepAsync(Page, "product-list-after-create");

        // Step 4: search by name + assert visible
        await list.SearchAsync(name);
        await Assertions.Expect(list.RowByName(name).First).ToBeVisibleAsync(new() { Timeout = Config.ActionTimeoutMs });
        await Artifacts.CaptureStepAsync(Page, "product-found-in-search");
    }

    [Test]
    public async Task Create_product_with_blank_name_shows_validation()
    {
        var list = new ProductsListPage(Page);
        await list.GoToAsync();
        await list.OpenNewAsync();
        var edit = new ProductEditPage(Page);
        await edit.SaveAsync();
        // DataAnnotations Required surfaces under the empty Name + Slug inputs.
        await Assertions.Expect(Page.GetByText("Name field is required").Or(Page.GetByText("required")).First)
            .ToBeVisibleAsync(new() { Timeout = Config.ActionTimeoutMs });
        await Artifacts.CaptureStepAsync(Page, "validation-required-fields");
    }

    [Test]
    public async Task Edit_a_just_created_product_changes_its_name()
    {
        var (name, slug, price) = TestDataFactory.NewProduct("edit");

        // Arrange — create one
        var list = new ProductsListPage(Page);
        await list.GoToAsync();
        await list.OpenNewAsync();
        var edit = new ProductEditPage(Page);
        await edit.FillRequiredAsync(name, slug, price);
        await edit.SaveAsync();
        await Page.WaitForURLAsync(u => u.Contains("/products") && !u.Contains("/create"));

        // Act — search, open, rename
        await list.SearchAsync(name);
        await list.OpenAsync(name);
        await Artifacts.CaptureStepAsync(Page, "edit-form-loaded");
        var newName = name + "-edited";
        await edit.NameField.FillAsync(newName);
        await Artifacts.CaptureStepAsync(Page, "edit-form-renamed");
        await edit.SaveAsync();
        await Page.WaitForURLAsync(u => u.Contains("/products") && !u.Contains("/edit"));

        // Assert
        await list.SearchAsync(newName);
        await Assertions.Expect(list.RowByName(newName).First).ToBeVisibleAsync();
        await Artifacts.CaptureStepAsync(Page, "edit-confirmed-in-list");
    }

    [Test]
    public async Task Delete_just_created_product_removes_it_from_list()
    {
        var (name, slug, price) = TestDataFactory.NewProduct("delete");
        var list = new ProductsListPage(Page);
        await list.GoToAsync();
        await list.OpenNewAsync();
        var edit = new ProductEditPage(Page);
        await edit.FillRequiredAsync(name, slug, price);
        await edit.SaveAsync();
        await Page.WaitForURLAsync(u => u.Contains("/products") && !u.Contains("/create"));

        await list.SearchAsync(name);
        await list.OpenAsync(name);
        await edit.DeleteButton.ClickAsync();
        await Page.WaitForURLAsync(u => u.Contains("/products") && !u.Contains("/edit"));

        await list.SearchAsync(name);
        await Assertions.Expect(list.RowByName(name)).ToHaveCountAsync(0);
        await Artifacts.CaptureStepAsync(Page, "product-gone-after-delete");
    }
}

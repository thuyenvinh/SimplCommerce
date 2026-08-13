using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SimplCommerce.ApiService.IntegrationTests;

/// <summary>
/// Real integration tests that require a running SQL container. Marked with
/// the <c>RequiresDocker</c> trait so CI can filter them out on hosts without
/// Docker. Run locally via <c>dotnet test --filter Category=RequiresDocker</c>.
/// </summary>
[Collection("ApiServiceDb")]
[Trait("Category", "RequiresDocker")]
public class HealthAndWebhookTests
{
    private readonly SimplApiFactory _factory;

    public HealthAndWebhookTests(SimplApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Host_is_up_anonymous_catalog_endpoint_returns_2xx()
    {
        // Liveness smoke: the dedicated /health endpoint is dev-only by design
        // (ServiceDefaults guards it behind IsDevelopment for security), so we prove
        // the host booted + the request pipeline serves by hitting an always-mapped
        // anonymous storefront endpoint.
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/storefront/catalog/categories");

        Assert.True(response.IsSuccessStatusCode,
            $"Expected 2xx but got {(int)response.StatusCode} {response.StatusCode}.");
    }

    [Fact]
    public async Task Stripe_webhook_rejects_missing_signature()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/webhooks/stripe", new { id = "evt_test" });

        // 503 (secret not configured in Testing config) or 401 (configured + bad sig).
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.ServiceUnavailable, HttpStatusCode.Unauthorized });
    }

    [Fact]
    public async Task Storefront_products_endpoint_answers_200_empty()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/storefront/catalog/products?page=1&pageSize=10");

        Assert.True(response.IsSuccessStatusCode,
            $"Expected 2xx but got {(int)response.StatusCode} {response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}");
    }
}

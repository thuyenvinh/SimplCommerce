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
    public async Task Health_ready_returns_healthy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

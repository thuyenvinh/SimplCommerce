using System.Net.Http.Json;

namespace SimplCommerce.Storefront.Services.ApiClients;

/// <summary>
/// Wave 19: storefront messaging client (Wave 15 backend). All endpoints are
/// CustomerOnly — registered with the bearer-forwarding handler. A customer's
/// thread is keyed by the vendor id they're talking to.
/// </summary>
public interface IMessagesApi
{
    Task<IReadOnlyList<VendorMessageThread>?> ListThreadsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<VendorMessage>?> GetThreadAsync(long vendorId, CancellationToken ct = default);
    Task<HttpResponseMessage> SendAsync(long vendorId, SendVendorMessageRequest req, CancellationToken ct = default);
}

public sealed class MessagesApi(HttpClient http) : IMessagesApi
{
    public async Task<IReadOnlyList<VendorMessageThread>?> ListThreadsAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<VendorMessageThread>>("/api/storefront/messages", ct);

    public async Task<IReadOnlyList<VendorMessage>?> GetThreadAsync(long vendorId, CancellationToken ct) =>
        await http.GetFromJsonAsync<List<VendorMessage>>($"/api/storefront/messages/{vendorId}", ct);

    public Task<HttpResponseMessage> SendAsync(long vendorId, SendVendorMessageRequest req, CancellationToken ct) =>
        http.PostAsJsonAsync($"/api/storefront/vendors/{vendorId}/messages", req, ct);
}

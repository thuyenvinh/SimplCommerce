using System.Net.Http.Headers;

namespace SimplCommerce.Admin.Services.Auth;

public sealed class ApiAuthDelegatingHandler(IHttpContextAccessor ctxAccessor) : DelegatingHandler
{
    public const string AccessTokenClaim = "api_access_token";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = ctxAccessor.HttpContext?.User?.FindFirst(AccessTokenClaim)?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

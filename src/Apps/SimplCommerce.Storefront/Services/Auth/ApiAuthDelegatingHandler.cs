using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace SimplCommerce.Storefront.Services.Auth;

/// <summary>
/// Forwards the JWT saved inside the cookie auth ticket onto every outgoing API call.
/// The token is stored under a private claim when the user signs in (see
/// <see cref="CookieAuthStateService.SignInWithTokenAsync"/>).
/// </summary>
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

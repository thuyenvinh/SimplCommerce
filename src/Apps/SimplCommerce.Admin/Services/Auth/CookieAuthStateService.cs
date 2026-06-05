using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SimplCommerce.Admin.Services.ApiClients;

namespace SimplCommerce.Admin.Services.Auth;

public sealed class CookieAuthStateService(IAuthApi authApi, IHttpContextAccessor ctxAccessor)
{
    public async Task<(bool success, string? error)> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var resp = await authApi.LoginAsync(new LoginRequest(email, password), ct);
        if (!resp.IsSuccessStatusCode)
        {
            return (false, "Invalid email or password.");
        }

        var payload = await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
        if (payload is null)
        {
            return (false, "Malformed login response.");
        }

        // Admin gate: the JWT must carry either admin or vendor role, otherwise the cookie
        // would still be issued but the admin app's fallback authorisation policy refuses
        // every page — confusing UX. Surface it here.
        if (!payload.Roles.Contains("admin", StringComparer.OrdinalIgnoreCase)
            && !payload.Roles.Contains("vendor", StringComparer.OrdinalIgnoreCase))
        {
            return (false, "This account is not authorised to use the admin panel.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(ApiAuthDelegatingHandler.AccessTokenClaim, payload.AccessToken),
        };
        foreach (var role in payload.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        if (ctxAccessor.HttpContext is { } http)
        {
            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = payload.ExpiresAt,
                });
        }

        return (true, null);
    }

    public Task SignOutAsync() =>
        ctxAccessor.HttpContext?.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
            ?? Task.CompletedTask;
}

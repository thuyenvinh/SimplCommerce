using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SimplCommerce.Storefront.Services.ApiClients;

namespace SimplCommerce.Storefront.Services.Auth;

/// <summary>
/// BFF-style glue: exchanges email/password against the ApiService's /api/auth/login
/// endpoint, then writes the resulting JWT into the cookie-backed auth ticket so
/// every subsequent request re-hydrates it automatically.
/// </summary>
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

        await SignInWithTokenAsync(email, payload);
        return (true, null);
    }

    public async Task<(bool success, string? error)> RegisterAsync(string email, string password, string fullName, CancellationToken ct = default)
    {
        var resp = await authApi.RegisterAsync(new RegisterRequest(email, password, fullName), ct);
        if (!resp.IsSuccessStatusCode)
        {
            return (false, "Registration failed.");
        }
        var payload = await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
        if (payload is null)
        {
            return (false, "Malformed register response.");
        }
        await SignInWithTokenAsync(email, payload);
        return (true, null);
    }

    public Task SignOutAsync() =>
        ctxAccessor.HttpContext?.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
            ?? Task.CompletedTask;

    private async Task SignInWithTokenAsync(string email, LoginResponse payload)
    {
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

        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = payload.ExpiresAt,
        };

        if (ctxAccessor.HttpContext is { } http)
        {
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
        }
    }
}

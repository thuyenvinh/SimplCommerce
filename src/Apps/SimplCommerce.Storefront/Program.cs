using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using SimplCommerce.Storefront.Components;
using SimplCommerce.Storefront.Services.ApiClients;
using SimplCommerce.Storefront.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ---- Razor Components (Blazor Web App: Interactive Server + WASM Auto) ----
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();

// ---- Cookie auth (Storefront acts as BFF in front of the ApiService) ----
// Customer logs in here, the server exchanges credentials for a JWT against the API,
// and keeps the JWT in the auth cookie. Outgoing HttpClient calls pick it back up via
// ApiAuthDelegatingHandler so the API-side [Authorize] sees the same principal.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "simpl.storefront.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// ---- Typed HttpClients to the ApiService (Aspire service discovery: http://api) ----
builder.Services.AddScoped<ApiAuthDelegatingHandler>();
var apiBase = builder.Configuration["services:api:https:0"]
    ?? builder.Configuration["services:api:http:0"]
    ?? "https+http://api";

builder.Services.AddHttpClient<ICatalogApi, CatalogApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<ISearchApi, SearchApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<ICartApi, CartApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAuthApi, AuthApi>(c => c.BaseAddress = new Uri(apiBase));
builder.Services.AddHttpClient<IAccountApi, AccountApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IOrderApi, OrderApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IWishlistApi, WishlistApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<ICmsApi, CmsApi>(c => c.BaseAddress = new Uri(apiBase));
builder.Services.AddHttpClient<INewsApi, NewsApi>(c => c.BaseAddress = new Uri(apiBase));

builder.Services.AddScoped<CookieAuthStateService>();

builder.Services.AddResponseCompression(o => o.EnableForHttps = true);
builder.Services.AddOutputCache();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SimplCommerce.Storefront.Client._Imports).Assembly);

// Simple health-friendly redirect for anyone landing on the auto-generated /weather route, etc.
app.MapGet("/sitemap.xml", () => Results.Text(
    """<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"></urlset>""",
    "application/xml"));
app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nAllow: /\nSitemap: /sitemap.xml", "text/plain"));

app.Run();

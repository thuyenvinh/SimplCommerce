using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using MudBlazor.Services;
using System.Globalization;
using SimplCommerce.Storefront.Components;
using SimplCommerce.Storefront.Services;
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
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Sitemap", b => b.Cache().Expire(TimeSpan.FromHours(1)));
});
builder.Services.AddScoped<SitemapBuilder>();

// ---- Localization (P4-37..P4-39) ----
// Cookie-based culture provider lets a logged-out visitor switch language without
// touching profile state; the Localization module's translation strings flow in via
// IStringLocalizer once a SharedResource resx is added under Resources/.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = (builder.Configuration.GetSection("Localization:Cultures").Get<string[]>()
    ?? new[] { "en-US", "vi-VN" })
    .Select(c => new CultureInfo(c))
    .ToArray();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(supportedCultures[0]);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

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
app.UseRequestLocalization();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SimplCommerce.Storefront.Client._Imports).Assembly);

// SEO endpoints: dynamic sitemap built from catalog slugs (1-hour OutputCache),
// robots.txt advertising it.
app.MapGet("/sitemap.xml", async (HttpContext ctx, SitemapBuilder builder, CancellationToken ct) =>
{
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    var xml = await builder.BuildAsync(baseUrl, ct);
    return Results.Text(xml, "application/xml");
}).CacheOutput("Sitemap");

app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nAllow: /\nSitemap: /sitemap.xml", "text/plain"));

// Cookie-based language switcher. Set the .AspNetCore.Culture cookie that
// CookieRequestCultureProvider reads on the next request, then redirect back.
// GET so a Blazor `NavigateTo(forceLoad: true)` works without antiforgery.
app.MapGet("/language", (string culture, string? returnUrl, HttpContext ctx) =>
{
    if (!supportedCultures.Any(c => c.Name == culture))
    {
        return Results.BadRequest("Unsupported culture.");
    }
    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, HttpOnly = false, SameSite = SameSiteMode.Lax });
    return Results.LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
});

app.Run();

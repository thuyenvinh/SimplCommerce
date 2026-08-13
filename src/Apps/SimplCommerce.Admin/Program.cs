using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using SimplCommerce.Admin.Components;
using SimplCommerce.Admin.Services.ApiClients;
using SimplCommerce.Admin.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Interactive Server only — the admin UX doesn't benefit from WASM prerender,
// and Blazor Server lets us lean on SignalR for live order notifications without
// going through the API indirectly.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SignalR + Redis backplane: when the ApiService broadcasts "order created"
// on /signalr (see Module.SignalR), every admin tab across every Admin server
// instance receives it. Dev falls back to in-memory if Redis isn't connected —
// Aspire provides the "redis" resource in production.
var signalR = builder.Services.AddSignalR();
var redisConn = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrWhiteSpace(redisConn))
{
    signalR.AddStackExchangeRedis(redisConn);
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
}

builder.Services.AddMudServices();

// Cookie auth — admin users exchange credentials for a JWT against the API,
// then the JWT is stashed as a private claim for downstream HttpClient calls.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "simpl.admin.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Per-route authorization is enforced by a small middleware (below, after
// UseAuthentication) that redirects anonymous browsers to /login for HTML
// routes while letting the Blazor circuit + login flow pass through. The
// old SetFallbackPolicy ALSO applied to /_blazor and blocked the SignalR
// circuit from establishing for anonymous users, so it's been replaced.
builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Typed HttpClients targeting /api/admin/* on the Aspire-discovered "api" resource.
builder.Services.AddScoped<ApiAuthDelegatingHandler>();
var apiBase = builder.Configuration["services:api:https:0"]
    ?? builder.Configuration["services:api:http:0"]
    ?? "https+http://api";

builder.Services.AddHttpClient<IAuthApi, AuthApi>(c => c.BaseAddress = new Uri(apiBase));
builder.Services.AddHttpClient<IAdminCatalogApi, AdminCatalogApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminOrdersApi, AdminOrdersApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminShipmentsApi, AdminShipmentsApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
// Named client for the <MediaPicker> shared component's multipart upload — carries
// the same bearer-forwarding handler so /api/media/upload's AdminOrVendor auth passes.
builder.Services.AddHttpClient("MediaApi", c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminCoreApi, AdminCoreApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminReviewsApi, AdminReviewsApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminInventoryApi, AdminInventoryApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminActivityApi, AdminActivityApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminVendorsApi, AdminVendorsApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
// Wave 18: vendor ↔ buyer messaging inbox client.
builder.Services.AddHttpClient<IAdminMessagesApi, AdminMessagesApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminTaxApi, AdminTaxApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminShippingApi, AdminShippingApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminPaymentsApi, AdminPaymentsApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminPricingApi, AdminPricingApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminCmsApi, AdminCmsApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminCommentsApi, AdminCommentsApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminContactsApi, AdminContactsApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminNewsApi, AdminNewsApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminLocalizationApi, AdminLocalizationApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();

builder.Services.AddScoped<CookieAuthStateService>();
builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

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

// Anonymous→/login gate for admin HTML routes. Whitelists the login flow,
// Blazor circuit, framework static assets, and the favicon so the SignalR
// handshake can actually open. Anything else gets a 302 to /login with a
// returnUrl back to the original path.
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? string.Empty;
    var isAuthed = ctx.User?.Identity?.IsAuthenticated == true;
    bool IsWhitelisted() =>
        path.StartsWith("/login", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/signin", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/favicon.png", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/app.css", StringComparison.OrdinalIgnoreCase);
    if (!isAuthed && !IsWhitelisted() && ctx.Request.Method == "GET")
    {
        var ret = Uri.EscapeDataString(path + ctx.Request.QueryString.Value);
        ctx.Response.Redirect($"/login?returnUrl={ret}");
        return;
    }
    await next();
});

// Sign-out endpoint. Plain HTTP redirect so HttpContext.SignOutAsync can
// clear the cookie before the response starts (the InteractiveServer version
// of /logout.razor hit the same "Headers are read-only" issue as Login).
app.MapGet("/logout", async (HttpContext http) =>
{
    await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
        http,
        Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).AllowAnonymous();

// Sign-in endpoint. The Login.razor form posts here so HttpContext.SignInAsync
// can set Set-Cookie BEFORE any rendering starts (impossible from inside a Razor
// component because the SSR pipeline has already begun writing the response body
// by the time HandleSubmit runs).
app.MapPost("/signin", async (
    HttpContext http,
    SimplCommerce.Admin.Services.Auth.CookieAuthStateService auth,
    [Microsoft.AspNetCore.Mvc.FromForm] string email,
    [Microsoft.AspNetCore.Mvc.FromForm] string password,
    [Microsoft.AspNetCore.Mvc.FromForm] string? returnUrl) =>
{
    var (ok, err) = await auth.SignInAsync(email ?? string.Empty, password ?? string.Empty);
    if (!ok)
    {
        var qs = $"?error={Uri.EscapeDataString(err ?? "Sign-in failed")}";
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            qs += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        }
        return Results.Redirect("/login" + qs);
    }
    return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
}).AllowAnonymous().DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Hub endpoint for AdminNotificationHub. Sits on this server (Admin) but the Redis
// backplane lets the ApiService publish notifications via IHubContext on its side.
app.MapHub<SimplCommerce.RealTime.AdminNotificationHub>("/hubs/admin-notifications");

app.Run();

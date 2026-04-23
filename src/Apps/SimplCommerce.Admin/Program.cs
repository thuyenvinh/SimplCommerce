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

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("admin", "vendor")
        .Build());

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
builder.Services.AddHttpClient<IAdminCoreApi, AdminCoreApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminReviewsApi, AdminReviewsApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminInventoryApi, AdminInventoryApi>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<ApiAuthDelegatingHandler>();
builder.Services.AddHttpClient<IAdminActivityApi, AdminActivityApi>(c => c.BaseAddress = new Uri(apiBase))
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

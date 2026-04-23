using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using SimplCommerce.Infrastructure;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.ActivityLog;
using SimplCommerce.Module.Catalog;
using SimplCommerce.Module.Checkouts;
using SimplCommerce.Module.Cms;
using SimplCommerce.Module.Comments;
using SimplCommerce.Module.Contacts;
using SimplCommerce.Module.Core;
using SimplCommerce.Module.Core.Data;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.DinkToPdf;
using SimplCommerce.Module.EmailSenderSmtp;
using SimplCommerce.Module.HangfireJobs;
using SimplCommerce.Module.Inventory;
using SimplCommerce.Module.Localization;
using SimplCommerce.Module.News;
using SimplCommerce.Module.Notifications;
using SimplCommerce.Module.Orders;
using SimplCommerce.Module.PaymentBraintree;
using SimplCommerce.Module.PaymentCashfree;
using SimplCommerce.Module.PaymentCoD;
using SimplCommerce.Module.PaymentMomo;
using SimplCommerce.Module.PaymentNganLuong;
using SimplCommerce.Module.PaymentPaypalExpress;
using SimplCommerce.Module.Payments;
using SimplCommerce.Module.PaymentStripe;
using SimplCommerce.Module.Pricing;
using SimplCommerce.Module.ProductComparison;
using SimplCommerce.Module.ProductRecentlyViewed;
using SimplCommerce.Module.Reviews;
using SimplCommerce.Module.SampleData;
using SimplCommerce.Module.Search;
using SimplCommerce.Module.Shipments;
using SimplCommerce.Module.Shipping;
using SimplCommerce.Module.ShippingFree;
using SimplCommerce.Module.ShippingPrices;
using SimplCommerce.Module.ShippingTableRate;
using SimplCommerce.Module.ShoppingCart;
using SimplCommerce.Module.SignalR;
using SimplCommerce.Module.StorageLocal;
using SimplCommerce.Module.Tax;
using SimplCommerce.Module.Vendors;
using SimplCommerce.Module.WishList;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ---- Infrastructure wiring (Aspire-injected connection strings) ----
builder.AddSqlServerDbContext<SimplDbContext>("SimplCommerce", configureDbContextOptions: options =>
{
    options.UseSqlServer(sql => sql.MigrationsAssembly("SimplCommerce.Migrations"));
});
builder.AddRedisDistributedCache("redis");
builder.AddAzureBlobServiceClient("blobs");

GlobalConfiguration.WebRootPath = builder.Environment.WebRootPath;
GlobalConfiguration.ContentRootPath = builder.Environment.ContentRootPath;

// ---- Identity (JWT-ready; no cookie middleware here — that's the Storefront/Admin BFF job) ----
builder.Services.AddIdentityCore<User>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 4;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<Role>()
    .AddEntityFrameworkStores<SimplDbContext>()
    .AddDefaultTokenProviders();

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "simplcommerce-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "simplcommerce-clients";
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? "dev-only-change-me-32-bytes-minimum!!";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("admin"))
    .AddPolicy("AdminOrVendor", p => p.RequireRole("admin", "vendor"))
    .AddPolicy("CustomerOnly", p => p.RequireAuthenticatedUser());

// ---- API surface (OpenAPI + Scalar, compression, output cache, CORS) ----
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "SimplCommerce API",
            Version = "v1",
            Description = "REST surface for SimplCommerce (storefront + admin)."
        };
        return Task.CompletedTask;
    });
});

builder.Services.AddResponseCompression(o => o.EnableForHttps = true);
builder.Services.AddOutputCache();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("https://localhost:5001", "https://localhost:5002")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddHttpClient();
builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddTransient(typeof(IRepositoryWithTypedId<,>), typeof(RepositoryWithTypedId<,>));

// MediatR scans every module assembly so every INotificationHandler<T> gets discovered
// without each Add<Module>Module() having to re-register handlers manually. The module
// extensions still register the ones they know about — duplicates are deduped by DI
// for services registered with identical (service type, impl type) pairs.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(SimplCommerce.Module.Core.CoreModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.Catalog.CatalogModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.Orders.OrdersModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.Inventory.InventoryModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.Cms.CmsModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.Reviews.ReviewsModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.Shipments.ShipmentsModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.Localization.LocalizationModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.ActivityLog.ActivityLogModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.ShoppingCart.ShoppingCartModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.ProductComparison.ProductComparisonModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.ProductRecentlyViewed.ProductRecentlyViewedModuleExtensions).Assembly,
        typeof(SimplCommerce.Module.Notifications.NotificationsModuleExtensions).Assembly);
});

// FluentValidation — discover validators across the solution. Each module's Application/
// folder is the intended home for IValidator<T> impls (Phase 3 per-endpoint work).
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---- Explicit module chain (no more reflection-driven ConfigureModules scan) ----
builder.Services
    .AddCoreModule()
    .AddLocalizationModule()
    .AddActivityLogModule()
    .AddTaxModule()
    .AddContactsModule()
    .AddVendorsModule()
    .AddCatalogModule()
    .AddCmsModule()
    .AddSearchModule()
    .AddNewsModule()
    .AddInventoryModule()
    .AddPricingModule()
    .AddShippingModule()
    .AddShippingPricesModule()
    .AddShippingFreeModule()
    .AddShippingTableRateModule()
    .AddShoppingCartModule()
    .AddCheckoutsModule()
    .AddOrdersModule()
    .AddShipmentsModule()
    .AddReviewsModule()
    .AddWishListModule()
    .AddProductComparisonModule()
    .AddProductRecentlyViewedModule()
    .AddPaymentsModule()
    .AddPaymentBraintreeModule()
    .AddPaymentCashfreeModule()
    .AddPaymentCoDModule()
    .AddPaymentMomoModule()
    .AddPaymentNganLuongModule()
    .AddPaymentPaypalExpressModule()
    .AddPaymentStripeModule()
    .AddCommentsModule()
    .AddSampleDataModule()
    .AddEmailSenderSmtpModule()
    .AddDinkToPdfModule()
    .AddStorageLocalModule()
    .AddNotificationsModule()
    .AddSignalRModule()
    .AddHangfireJobsModule(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o =>
    {
        o.Title = "SimplCommerce API";
    });
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapSignalRModule();

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();

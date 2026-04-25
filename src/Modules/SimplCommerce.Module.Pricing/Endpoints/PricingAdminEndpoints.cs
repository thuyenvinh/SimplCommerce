#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Pricing.Models;

namespace SimplCommerce.Module.Pricing.Endpoints;

public static class PricingAdminEndpoints
{
    public static IEndpointRouteBuilder MapPricingAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/pricing")
            .WithTags("Admin.Pricing")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/cart-rules", async (IRepository<CartRule> repo) =>
            Results.Ok(await repo.Query().Select(r => new { r.Id, r.Name, r.StartOn, r.EndOn, r.UsageLimitPerCoupon, r.IsActive }).ToListAsync()));

        group.MapGet("/catalog-rules", async (IRepository<CatalogRule> repo) =>
            Results.Ok(await repo.Query().Select(r => new { r.Id, r.Name, r.StartOn, r.EndOn, r.IsActive }).ToListAsync()));

        group.MapGet("/coupons", async (IRepository<Coupon> repo) =>
            Results.Ok(await repo.Query().Select(c => new { c.Id, c.Code, c.CartRuleId }).ToListAsync()));

        return app;
    }
}

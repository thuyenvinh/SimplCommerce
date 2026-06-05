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
    public record CartRuleInput(string Name, string? Description, bool IsActive, System.DateTimeOffset? StartOn, System.DateTimeOffset? EndOn,
        bool IsCouponRequired, string RuleToApply, decimal DiscountAmount, decimal? MaxDiscountAmount,
        int? UsageLimitPerCoupon, int? UsageLimitPerCustomer);

    public record CartRuleDetail(long Id, string Name, string? Description, bool IsActive,
        System.DateTimeOffset? StartOn, System.DateTimeOffset? EndOn, bool IsCouponRequired,
        string RuleToApply, decimal DiscountAmount, decimal? MaxDiscountAmount,
        int? UsageLimitPerCoupon, int? UsageLimitPerCustomer);

    public record CouponInput(long CartRuleId, string Code);

    public static IEndpointRouteBuilder MapPricingAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/pricing")
            .WithTags("Admin.Pricing")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/cart-rules", async (IRepository<CartRule> repo) =>
            Results.Ok(await repo.Query().Select(r => new { r.Id, r.Name, r.StartOn, r.EndOn, r.UsageLimitPerCoupon, r.IsActive }).ToListAsync()));

        group.MapGet("/cart-rules/{id:long}", async (long id, IRepository<CartRule> repo) =>
        {
            var rule = await repo.Query().FirstOrDefaultAsync(r => r.Id == id);
            if (rule is null) return Results.NotFound();
            return Results.Ok(new CartRuleDetail(rule.Id, rule.Name, rule.Description, rule.IsActive,
                rule.StartOn, rule.EndOn, rule.IsCouponRequired, rule.RuleToApply, rule.DiscountAmount,
                rule.MaxDiscountAmount, rule.UsageLimitPerCoupon, rule.UsageLimitPerCustomer));
        });

        group.MapPost("/cart-rules", async (CartRuleInput input, IRepository<CartRule> repo) =>
        {
            var rule = Apply(new CartRule(), input);
            repo.Add(rule);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/pricing/cart-rules/{rule.Id}", new { rule.Id });
        });

        group.MapPut("/cart-rules/{id:long}", async (long id, CartRuleInput input, IRepository<CartRule> repo) =>
        {
            var rule = await repo.Query().FirstOrDefaultAsync(r => r.Id == id);
            if (rule is null) return Results.NotFound();
            Apply(rule, input);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/cart-rules/{id:long}", async (long id, IRepository<CartRule> repo) =>
        {
            var rule = await repo.Query().FirstOrDefaultAsync(r => r.Id == id);
            if (rule is null) return Results.NotFound();
            repo.Remove(rule);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapGet("/catalog-rules", async (IRepository<CatalogRule> repo) =>
            Results.Ok(await repo.Query().Select(r => new { r.Id, r.Name, r.StartOn, r.EndOn, r.IsActive }).ToListAsync()));

        group.MapGet("/coupons", async (IRepository<Coupon> repo) =>
            Results.Ok(await repo.Query().Select(c => new { c.Id, c.Code, c.CartRuleId }).ToListAsync()));

        group.MapPost("/coupons", async (CouponInput input, IRepository<Coupon> coupons, IRepository<CartRule> rules) =>
        {
            if (string.IsNullOrWhiteSpace(input.Code))
            {
                return Results.BadRequest(new { error = "Code is required." });
            }
            if (!await rules.Query().AnyAsync(r => r.Id == input.CartRuleId))
            {
                return Results.BadRequest(new { error = "CartRuleId does not resolve to a CartRule." });
            }
            if (await coupons.Query().AnyAsync(c => c.Code == input.Code))
            {
                return Results.Conflict(new { error = "Coupon code already exists." });
            }
            var coupon = new Coupon { Code = input.Code, CartRuleId = input.CartRuleId };
            coupons.Add(coupon);
            await coupons.SaveChangesAsync();
            return Results.Created($"/api/admin/pricing/coupons/{coupon.Id}", new { coupon.Id });
        });

        group.MapDelete("/coupons/{id:long}", async (long id, IRepository<Coupon> repo) =>
        {
            var coupon = await repo.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (coupon is null) return Results.NotFound();
            repo.Remove(coupon);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    private static CartRule Apply(CartRule rule, CartRuleInput input)
    {
        rule.Name = input.Name;
        rule.Description = input.Description ?? string.Empty;
        rule.IsActive = input.IsActive;
        rule.StartOn = input.StartOn;
        rule.EndOn = input.EndOn;
        rule.IsCouponRequired = input.IsCouponRequired;
        rule.RuleToApply = input.RuleToApply;
        rule.DiscountAmount = input.DiscountAmount;
        rule.MaxDiscountAmount = input.MaxDiscountAmount;
        rule.UsageLimitPerCoupon = input.UsageLimitPerCoupon;
        rule.UsageLimitPerCustomer = input.UsageLimitPerCustomer;
        return rule;
    }
}

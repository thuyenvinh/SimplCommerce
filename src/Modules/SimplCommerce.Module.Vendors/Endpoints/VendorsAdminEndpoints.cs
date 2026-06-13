#nullable enable
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Infrastructure.Web;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Vendors.Models;

namespace SimplCommerce.Module.Vendors.Endpoints;

public static class VendorsAdminEndpoints
{
    public record VendorInput(string Name, string Slug, string? Description, string? Email, bool IsActive, decimal CommissionPercent = 0m);
    public record VendorDetail(long Id, string Name, string Slug, string? Description, string? Email, bool IsActive, decimal CommissionPercent, System.DateTimeOffset CreatedOn);

    // Wave 9: dashboard "self" endpoint. Vendor logs in, gets back their own
    // profile + pending balance for the dashboard top strip. AdminOnly tier
    // returns 204 — the dashboard then renders its admin view instead.
    public record VendorSelfResponse(VendorDetail Vendor, VendorBalanceSummary Balance);

    // Wave 8: payout reporting + creation
    public record VendorBalanceSummary(long VendorId, string VendorName, decimal PendingPayoutGross, decimal PendingCommission, int EligibleOrderCount);
    public record CreatePayoutRequest(string? ExternalTransferReference, string? Note);
    public record PayoutItem(long Id, long VendorId, DateTimeOffset CreatedOn, decimal GrossAmount, decimal CommissionAmount, decimal NetAmount, int OrderCount, string? ExternalTransferReference);

    public static IEndpointRouteBuilder MapVendorsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/vendors")
            .WithTags("Admin.Vendors")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IRepository<Vendor> repo) =>
        {
            var list = await repo.Query().Where(v => !v.IsDeleted)
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name, v.Slug, v.Description, v.IsActive })
                .ToListAsync();
            return Results.Ok(list);
        });

        // Wave 9: vendor dashboard bootstrap. Both admin and vendor can hit this;
        // admin gets 204 (no vendor context), vendor gets profile + pending balance.
        group.MapGet("/me", async (IRepository<Vendor> vendors, IRepository<Order> orders, IVendorScope scope) =>
        {
            if (scope.CurrentVendorId is not { } vid) return Results.NoContent();
            var v = await vendors.Query().FirstOrDefaultAsync(x => x.Id == vid && !x.IsDeleted);
            if (v is null) return Results.NotFound();
            var eligible = orders.Query()
                .Where(o => o.VendorId == vid
                    && o.OrderStatus == OrderStatus.Complete
                    && o.VendorPayoutId == null);
            var grossSubtotal = await eligible.SumAsync(o => (decimal?)o.SubTotal) ?? 0m;
            var commission = await eligible.SumAsync(o => (decimal?)o.CommissionAmount) ?? 0m;
            var count = await eligible.CountAsync();
            return Results.Ok(new VendorSelfResponse(
                new VendorDetail(v.Id, v.Name, v.Slug, v.Description, v.Email, v.IsActive, v.CommissionPercent, v.CreatedOn),
                new VendorBalanceSummary(v.Id, v.Name, grossSubtotal, commission, count)));
        }).RequireAuthorization("AdminOrVendor");

        group.MapGet("/{id:long}", async (long id, IRepository<Vendor> repo) =>
        {
            var v = await repo.Query().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return v is null
                ? Results.NotFound()
                : Results.Ok(new VendorDetail(v.Id, v.Name, v.Slug, v.Description, v.Email, v.IsActive, v.CommissionPercent, v.CreatedOn));
        });

        group.MapPost("/", async (VendorInput input, IRepository<Vendor> repo) =>
        {
            if (input.CommissionPercent < 0m || input.CommissionPercent > 100m)
            {
                return Results.BadRequest(new { error = "CommissionPercent must be 0-100." });
            }
            var vendor = new Vendor
            {
                Name = input.Name,
                Slug = input.Slug,
                Description = input.Description ?? string.Empty,
                Email = input.Email ?? string.Empty,
                IsActive = input.IsActive,
                CommissionPercent = input.CommissionPercent,
            };
            repo.Add(vendor);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/vendors/{vendor.Id}", new { vendor.Id });
        });

        group.MapPut("/{id:long}", async (long id, VendorInput input, IRepository<Vendor> repo) =>
        {
            if (input.CommissionPercent < 0m || input.CommissionPercent > 100m)
            {
                return Results.BadRequest(new { error = "CommissionPercent must be 0-100." });
            }
            var vendor = await repo.Query().FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (vendor is null) return Results.NotFound();
            vendor.Name = input.Name;
            vendor.Slug = input.Slug;
            vendor.Description = input.Description ?? string.Empty;
            vendor.Email = input.Email ?? string.Empty;
            vendor.IsActive = input.IsActive;
            // Updating CommissionPercent only affects future orders. Past sub-orders
            // already have CommissionAmount stamped at the rate-at-the-time; we
            // never retroactively re-rate completed transactions.
            vendor.CommissionPercent = input.CommissionPercent;
            vendor.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:long}", async (long id, IRepository<Vendor> repo) =>
        {
            var vendor = await repo.Query().FirstOrDefaultAsync(v => v.Id == id);
            if (vendor is null) return Results.NotFound();
            vendor.IsDeleted = true;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---- Wave 8: payouts ----
        // Eligible-for-payout sub-order = vendor's row, Completed status, not yet
        // assigned to a VendorPayoutId. We restrict to Complete (not just
        // PaymentReceived) so admins don't pay out on orders that may still
        // get refunded — keeps the platform's exposure small.
        group.MapGet("/{id:long}/balance", async (long id, IRepository<Vendor> vendors, IRepository<Order> orders) =>
        {
            var v = await vendors.Query().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (v is null) return Results.NotFound();
            var eligible = orders.Query()
                .Where(o => o.VendorId == id
                    && o.OrderStatus == OrderStatus.Complete
                    && o.VendorPayoutId == null);
            var grossSubtotal = await eligible.SumAsync(o => (decimal?)o.SubTotal) ?? 0m;
            var commission = await eligible.SumAsync(o => (decimal?)o.CommissionAmount) ?? 0m;
            var count = await eligible.CountAsync();
            return Results.Ok(new VendorBalanceSummary(v.Id, v.Name, grossSubtotal, commission, count));
        });

        group.MapGet("/{id:long}/payouts", async (long id, IRepository<VendorPayout> repo, int page = 1, int pageSize = 20) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = repo.Query().Where(p => p.VendorId == id);
            var total = await query.CountAsync();
            var rows = await query.OrderByDescending(p => p.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new PayoutItem(p.Id, p.VendorId, p.CreatedOn, p.GrossAmount, p.CommissionAmount, p.NetAmount, p.OrderCount, p.ExternalTransferReference))
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        group.MapPost("/{id:long}/payouts", async (
            long id,
            CreatePayoutRequest req,
            IRepository<Vendor> vendors,
            IRepository<Order> orders,
            IRepository<VendorPayout> payouts,
            ClaimsPrincipal principal) =>
        {
            var vendor = await vendors.Query().FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (vendor is null) return Results.NotFound();
            var rawUserId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;
            long.TryParse(rawUserId, out var adminId);

            var eligibleOrders = await orders.Query()
                .Where(o => o.VendorId == id
                    && o.OrderStatus == OrderStatus.Complete
                    && o.VendorPayoutId == null)
                .ToListAsync();
            if (eligibleOrders.Count == 0)
            {
                return Results.BadRequest(new { error = "No eligible orders to pay out." });
            }
            var gross = eligibleOrders.Sum(o => o.SubTotal);
            var commission = eligibleOrders.Sum(o => o.CommissionAmount);
            var payout = new VendorPayout
            {
                VendorId = vendor.Id,
                CreatedByUserId = adminId,
                GrossAmount = gross,
                CommissionAmount = commission,
                NetAmount = gross - commission,
                OrderCount = eligibleOrders.Count,
                ExternalTransferReference = req.ExternalTransferReference ?? string.Empty,
                Note = req.Note ?? string.Empty,
            };
            payouts.Add(payout);
            await payouts.SaveChangesAsync();

            // Stamp the payout link onto each included order. Doing this after
            // the payout SaveChanges so we have the FK id; both writes share the
            // same DbContext so a single transaction would also work.
            foreach (var o in eligibleOrders)
            {
                o.VendorPayoutId = payout.Id;
            }
            await orders.SaveChangesAsync();

            return Results.Created($"/api/admin/vendors/{id}/payouts/{payout.Id}",
                new PayoutItem(payout.Id, payout.VendorId, payout.CreatedOn, payout.GrossAmount,
                    payout.CommissionAmount, payout.NetAmount, payout.OrderCount, payout.ExternalTransferReference));
        });

        return app;
    }
}

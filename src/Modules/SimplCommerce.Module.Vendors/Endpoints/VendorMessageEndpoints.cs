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
using SimplCommerce.Module.Vendors.Models;

namespace SimplCommerce.Module.Vendors.Endpoints;

/// <summary>
/// Wave 15: buyer ↔ vendor messaging.
///
/// Customer side (storefront):
///   POST /api/storefront/vendors/{vendorId}/messages — send a message to a vendor
///   GET  /api/storefront/messages                    — customer's own threads
///                                                       (one row per vendor counterpart)
///   GET  /api/storefront/messages/{vendorId}         — full thread between customer
///                                                       and one vendor
///
/// Vendor / admin side:
///   GET  /api/admin/messages                         — vendor's inbox
///                                                       (one row per customer counterpart;
///                                                       vendor-scoped via Wave 6)
///   GET  /api/admin/messages/{customerUserId}        — full thread with one customer
///   POST /api/admin/messages/{customerUserId}        — vendor / admin reply
/// </summary>
public static class VendorMessageEndpoints
{
    public record SendMessageRequest(string Body, long? OrderId);
    public record MessageItem(long Id, int SenderKind, long FromUserId, string? FromName, long? OrderId, string Body, DateTimeOffset CreatedOn, DateTimeOffset? ReadAt);
    public record ThreadSummary(long CounterpartUserId, string? CounterpartName, long VendorId, string VendorName, DateTimeOffset LastMessageOn, string LastSnippet, int UnreadCount);

    public static IEndpointRouteBuilder MapVendorMessageStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront")
            .WithTags("Storefront.Messages")
            .RequireAuthorization("CustomerOnly");

        group.MapPost("/vendors/{vendorId:long}/messages", async (
            long vendorId,
            SendMessageRequest req,
            IRepository<Vendor> vendors,
            IRepository<VendorMessage> messages,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(req.Body)) return Results.BadRequest(new { error = "Body required." });
            if (req.Body.Length > 4000) return Results.BadRequest(new { error = "Body exceeds 4000 chars." });
            var vendor = await vendors.Query().FirstOrDefaultAsync(v => v.Id == vendorId && !v.IsDeleted && v.IsActive);
            if (vendor is null) return Results.NotFound();
            var msg = new VendorMessage
            {
                VendorId = vendorId,
                CustomerUserId = userId,
                FromUserId = userId,
                SenderKind = VendorMessageSenderKind.Customer,
                OrderId = req.OrderId,
                Body = req.Body,
            };
            messages.Add(msg);
            await messages.SaveChangesAsync();
            return Results.Created($"/api/storefront/messages/{vendorId}", new { msg.Id });
        });

        group.MapGet("/messages", async (
            IRepository<VendorMessage> messages,
            IRepository<Vendor> vendors,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            // Group by vendor — last message + unread count for this customer.
            // "Unread for me" = messages where I'm NOT the sender and ReadAt is null.
            var threads = await messages.Query()
                .Where(m => m.CustomerUserId == userId)
                .GroupBy(m => m.VendorId)
                .Select(g => new
                {
                    VendorId = g.Key,
                    LastOn = g.Max(x => x.CreatedOn),
                    Last = g.OrderByDescending(x => x.CreatedOn).First(),
                    Unread = g.Count(x => x.SenderKind != VendorMessageSenderKind.Customer && x.ReadAt == null),
                })
                .ToListAsync();
            var vendorIds = threads.Select(t => t.VendorId).ToList();
            var vendorMap = await vendors.Query().Where(v => vendorIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.Name);
            var items = threads
                .OrderByDescending(t => t.LastOn)
                .Select(t => new ThreadSummary(
                    CounterpartUserId: 0,
                    CounterpartName: null,
                    VendorId: t.VendorId,
                    VendorName: vendorMap.TryGetValue(t.VendorId, out var n) ? n : string.Empty,
                    LastMessageOn: t.LastOn,
                    LastSnippet: Snippet(t.Last.Body),
                    UnreadCount: t.Unread))
                .ToList();
            return Results.Ok(items);
        });

        group.MapGet("/messages/{vendorId:long}", async (
            long vendorId,
            IRepository<VendorMessage> messages,
            IRepository<User> users,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var rows = await messages.Query()
                .Where(m => m.VendorId == vendorId && m.CustomerUserId == userId)
                .OrderBy(m => m.CreatedOn)
                .ToListAsync();
            // Mark vendor/admin replies as read on this fetch.
            var now = DateTimeOffset.UtcNow;
            foreach (var m in rows.Where(m => m.SenderKind != VendorMessageSenderKind.Customer && m.ReadAt is null))
            {
                m.ReadAt = now;
            }
            await messages.SaveChangesAsync();
            var fromUserIds = rows.Select(m => m.FromUserId).Distinct().ToList();
            var nameMap = await users.Query().Where(u => fromUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);
            return Results.Ok(rows.Select(m => new MessageItem(
                m.Id, (int)m.SenderKind, m.FromUserId,
                nameMap.TryGetValue(m.FromUserId, out var fn) ? fn : null,
                m.OrderId, m.Body, m.CreatedOn, m.ReadAt)).ToList());
        });

        return app;
    }

    public static IEndpointRouteBuilder MapVendorMessageAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/messages")
            .WithTags("Admin.Messages")
            .RequireAuthorization("AdminOrVendor");

        group.MapGet("/", async (
            IRepository<VendorMessage> messages,
            IRepository<User> users,
            IVendorScope scope) =>
        {
            var query = messages.Query();
            if (scope.CurrentVendorId is { } vid) query = query.Where(m => m.VendorId == vid);
            var threads = await query
                .GroupBy(m => new { m.VendorId, m.CustomerUserId })
                .Select(g => new
                {
                    g.Key.VendorId,
                    g.Key.CustomerUserId,
                    LastOn = g.Max(x => x.CreatedOn),
                    Last = g.OrderByDescending(x => x.CreatedOn).First(),
                    Unread = g.Count(x => x.SenderKind == VendorMessageSenderKind.Customer && x.ReadAt == null),
                })
                .ToListAsync();
            var customerIds = threads.Select(t => t.CustomerUserId).Distinct().ToList();
            var nameMap = await users.Query().Where(u => customerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);
            return Results.Ok(threads
                .OrderByDescending(t => t.LastOn)
                .Select(t => new ThreadSummary(
                    CounterpartUserId: t.CustomerUserId,
                    CounterpartName: nameMap.TryGetValue(t.CustomerUserId, out var n) ? n : null,
                    VendorId: t.VendorId,
                    VendorName: string.Empty,
                    LastMessageOn: t.LastOn,
                    LastSnippet: Snippet(t.Last.Body),
                    UnreadCount: t.Unread))
                .ToList());
        });

        group.MapGet("/{customerUserId:long}", async (
            long customerUserId,
            IRepository<VendorMessage> messages,
            IRepository<User> users,
            IVendorScope scope) =>
        {
            var query = messages.Query().Where(m => m.CustomerUserId == customerUserId);
            if (scope.CurrentVendorId is { } vid) query = query.Where(m => m.VendorId == vid);
            var rows = await query.OrderBy(m => m.CreatedOn).ToListAsync();
            var now = DateTimeOffset.UtcNow;
            foreach (var m in rows.Where(m => m.SenderKind == VendorMessageSenderKind.Customer && m.ReadAt is null))
            {
                m.ReadAt = now;
            }
            await messages.SaveChangesAsync();
            var fromUserIds = rows.Select(m => m.FromUserId).Distinct().ToList();
            var nameMap = await users.Query().Where(u => fromUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);
            return Results.Ok(rows.Select(m => new MessageItem(
                m.Id, (int)m.SenderKind, m.FromUserId,
                nameMap.TryGetValue(m.FromUserId, out var fn) ? fn : null,
                m.OrderId, m.Body, m.CreatedOn, m.ReadAt)).ToList());
        });

        group.MapPost("/{customerUserId:long}", async (
            long customerUserId,
            SendMessageRequest req,
            IRepository<VendorMessage> messages,
            IVendorScope scope,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(req.Body)) return Results.BadRequest(new { error = "Body required." });
            if (req.Body.Length > 4000) return Results.BadRequest(new { error = "Body exceeds 4000 chars." });
            // Vendor reply uses scope.CurrentVendorId; platform-admin reply must
            // supply the targeted vendor via a query param. For now we infer
            // from the existing thread's VendorId.
            var existing = await messages.Query()
                .Where(m => m.CustomerUserId == customerUserId)
                .Where(m => scope.CurrentVendorId == null || m.VendorId == scope.CurrentVendorId)
                .OrderByDescending(m => m.CreatedOn)
                .FirstOrDefaultAsync();
            if (existing is null) return Results.BadRequest(new { error = "No existing thread; admin reply requires an existing customer message." });
            var senderKind = scope.CurrentVendorId.HasValue
                ? VendorMessageSenderKind.Vendor
                : VendorMessageSenderKind.Admin;
            var msg = new VendorMessage
            {
                VendorId = existing.VendorId,
                CustomerUserId = customerUserId,
                FromUserId = userId,
                SenderKind = senderKind,
                OrderId = req.OrderId,
                Body = req.Body,
            };
            messages.Add(msg);
            await messages.SaveChangesAsync();
            return Results.Created($"/api/admin/messages/{customerUserId}", new { msg.Id });
        });

        return app;
    }

    private static string Snippet(string body)
        => body.Length <= 120 ? body : body[..120] + "…";

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId)
    {
        var raw = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(raw, out userId);
    }
}

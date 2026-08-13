#nullable enable
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Vendors.Models;

namespace SimplCommerce.Module.Vendors.Endpoints;

/// <summary>
/// Wave 7: marketplace onboarding queue.
///
/// Storefront side:
///   POST /api/storefront/vendors/apply — customer submits business details.
///                                        One open application per user at a time
///                                        (re-submit replaces a previous Rejected
///                                        but cannot overlap with a Pending one).
///
/// Admin side:
///   GET   /api/admin/vendors/applications                — list with status filter
///   POST  /api/admin/vendors/applications/{id}/approve   — creates Vendor + sets
///                                                          User.VendorId + assigns
///                                                          "vendor" role atomically.
///   POST  /api/admin/vendors/applications/{id}/reject    — sets Status=Rejected
///                                                          with a decision note;
///                                                          applicant can re-apply.
/// </summary>
public static class VendorApplicationEndpoints
{
    public record ApplyRequest(
        string BusinessName,
        string Slug,
        string? Description,
        string? ContactEmail,
        string? ContactPhone,
        string? RegistrationNote);

    public record ApplicationItem(
        long Id, long ApplicantUserId, string? ApplicantEmail,
        string BusinessName, string Slug, string? Description,
        string? ContactEmail, string? ContactPhone,
        int Status, DateTimeOffset CreatedOn,
        long? DecidedByUserId, DateTimeOffset? DecidedOn, string? DecisionNote,
        long? CreatedVendorId);

    public record DecisionRequest(string? Note);

    public static IEndpointRouteBuilder MapVendorApplicationStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/vendors")
            .WithTags("Storefront.Vendors")
            .RequireAuthorization("CustomerOnly");

        group.MapPost("/apply", async (
            ApplyRequest req,
            IRepository<VendorApplication> apps,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(req.BusinessName) || string.IsNullOrWhiteSpace(req.Slug))
            {
                return Results.BadRequest(new { error = "BusinessName and Slug are required." });
            }

            // Reject overlapping pending applications. A user with a Rejected
            // record can re-apply — that's the whole point of the queue.
            var hasPending = await apps.Query()
                .AnyAsync(a => a.ApplicantUserId == userId && a.Status == VendorApplicationStatus.Pending);
            if (hasPending)
            {
                return Results.Conflict(new { error = "You already have a pending application." });
            }

            var slugTaken = await apps.Query()
                .AnyAsync(a => a.Slug == req.Slug && a.Status != VendorApplicationStatus.Rejected);
            if (slugTaken)
            {
                return Results.Conflict(new { error = "That slug is already in use.", field = nameof(req.Slug) });
            }

            var entity = new VendorApplication
            {
                ApplicantUserId = userId,
                BusinessName = req.BusinessName,
                Slug = req.Slug,
                Description = req.Description ?? string.Empty,
                ContactEmail = req.ContactEmail ?? string.Empty,
                ContactPhone = req.ContactPhone ?? string.Empty,
                RegistrationNote = req.RegistrationNote ?? string.Empty,
            };
            apps.Add(entity);
            await apps.SaveChangesAsync();
            return Results.Created($"/api/admin/vendors/applications/{entity.Id}", new { entity.Id });
        });

        group.MapGet("/my-application", async (
            IRepository<VendorApplication> apps,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var entity = await apps.Query()
                .Where(a => a.ApplicantUserId == userId)
                .OrderByDescending(a => a.CreatedOn)
                .FirstOrDefaultAsync();
            if (entity is null) return Results.NoContent();
            return Results.Ok(new
            {
                entity.Id, entity.BusinessName, entity.Slug,
                Status = (int)entity.Status, entity.CreatedOn,
                entity.DecidedOn, entity.DecisionNote, entity.CreatedVendorId,
            });
        });

        return app;
    }

    public static IEndpointRouteBuilder MapVendorApplicationAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/vendors/applications")
            .WithTags("Admin.Vendors.Applications")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IRepository<VendorApplication> apps, int? status = null, int page = 1, int pageSize = 20) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = apps.Query().Include(a => a.ApplicantUser).AsQueryable();
            if (status.HasValue) query = query.Where(a => (int)a.Status == status);
            var total = await query.CountAsync();
            var rows = await query.OrderByDescending(a => a.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new ApplicationItem(
                    a.Id, a.ApplicantUserId, a.ApplicantUser != null ? a.ApplicantUser.Email : null,
                    a.BusinessName, a.Slug, a.Description, a.ContactEmail, a.ContactPhone,
                    (int)a.Status, a.CreatedOn,
                    a.DecidedByUserId, a.DecidedOn, a.DecisionNote, a.CreatedVendorId))
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        group.MapGet("/{id:long}", async (long id, IRepository<VendorApplication> apps) =>
        {
            var a = await apps.Query().Include(x => x.ApplicantUser)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return Results.NotFound();
            return Results.Ok(new ApplicationItem(
                a.Id, a.ApplicantUserId, a.ApplicantUser?.Email,
                a.BusinessName, a.Slug, a.Description, a.ContactEmail, a.ContactPhone,
                (int)a.Status, a.CreatedOn,
                a.DecidedByUserId, a.DecidedOn, a.DecisionNote, a.CreatedVendorId));
        });

        group.MapPost("/{id:long}/approve", async (
            long id,
            DecisionRequest req,
            IRepository<VendorApplication> apps,
            IRepository<Vendor> vendors,
            UserManager<User> users,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var adminId)) return Results.Unauthorized();
            var app = await apps.Query().FirstOrDefaultAsync(a => a.Id == id);
            if (app is null) return Results.NotFound();
            if (app.Status != VendorApplicationStatus.Pending)
            {
                return Results.BadRequest(new { error = "Application is not pending.", currentStatus = (int)app.Status });
            }
            var applicant = await users.FindByIdAsync(app.ApplicantUserId.ToString());
            if (applicant is null)
            {
                return Results.BadRequest(new { error = "Applicant user no longer exists." });
            }
            if (applicant.VendorId.HasValue)
            {
                return Results.BadRequest(new { error = "Applicant already owns a vendor.", existingVendorId = applicant.VendorId });
            }

            // Atomicity: Vendor insert + user role + VendorId stamp must succeed
            // together. SimplDbContext shares the connection across repositories
            // so a single transaction covers the Vendor + User updates; Identity's
            // AddToRoleAsync runs against the same context via UserManager.
            var vendor = new Vendor
            {
                Name = app.BusinessName,
                Slug = app.Slug,
                Description = app.Description ?? string.Empty,
                Email = string.IsNullOrWhiteSpace(app.ContactEmail) ? (applicant.Email ?? string.Empty) : app.ContactEmail,
                IsActive = true,
            };
            vendors.Add(vendor);
            await vendors.SaveChangesAsync();

            applicant.VendorId = vendor.Id;
            var updateResult = await users.UpdateAsync(applicant);
            if (!updateResult.Succeeded)
            {
                // Roll back the vendor row if we can't promote the user — better
                // a leftover failed application than an orphan vendor + a regular
                // customer with no Identity link.
                vendor.IsDeleted = true;
                await vendors.SaveChangesAsync();
                return Results.Problem(detail: string.Join("; ", updateResult.Errors.Select(e => e.Description)));
            }
            if (!await users.IsInRoleAsync(applicant, "vendor"))
            {
                await users.AddToRoleAsync(applicant, "vendor");
            }

            app.Status = VendorApplicationStatus.Approved;
            app.DecidedByUserId = adminId;
            app.DecidedOn = DateTimeOffset.UtcNow;
            app.DecisionNote = req.Note ?? string.Empty;
            app.CreatedVendorId = vendor.Id;
            app.LatestUpdatedOn = DateTimeOffset.UtcNow;
            await apps.SaveChangesAsync();

            return Results.Ok(new { vendorId = vendor.Id, applicationStatus = (int)app.Status });
        });

        group.MapPost("/{id:long}/reject", async (
            long id,
            DecisionRequest req,
            IRepository<VendorApplication> apps,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var adminId)) return Results.Unauthorized();
            var app = await apps.Query().FirstOrDefaultAsync(a => a.Id == id);
            if (app is null) return Results.NotFound();
            if (app.Status != VendorApplicationStatus.Pending)
            {
                return Results.BadRequest(new { error = "Application is not pending.", currentStatus = (int)app.Status });
            }
            app.Status = VendorApplicationStatus.Rejected;
            app.DecidedByUserId = adminId;
            app.DecidedOn = DateTimeOffset.UtcNow;
            app.DecisionNote = req.Note ?? string.Empty;
            app.LatestUpdatedOn = DateTimeOffset.UtcNow;
            await apps.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId)
    {
        var raw = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(raw, out userId);
    }
}

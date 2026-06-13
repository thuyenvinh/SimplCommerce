#nullable enable
using System;
using System.IO;
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
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Vendors.Models;

namespace SimplCommerce.Module.Vendors.Endpoints;

/// <summary>
/// Wave 14: vendor KYC document workflow.
///
/// Storefront:
///   POST /api/storefront/vendors/applications/{appId}/documents
///        Customer uploads a registration / id / tax doc tied to their pending
///        VendorApplication. Multipart form: file + documentType.
///
/// Admin:
///   GET   /api/admin/vendors/applications/{appId}/documents
///   GET   /api/admin/vendors/{vendorId}/documents
///   PATCH /api/admin/vendors/documents/{docId}/status — verify / reject + note
/// </summary>
public static class VendorDocumentEndpoints
{
    public record DocumentItem(long Id, int DocumentType, int Status, long MediaId, string Url, DateTimeOffset CreatedOn, long? VendorApplicationId, long? VendorId, DateTimeOffset? VerifiedOn, string? AdminNote);
    public record StatusUpdateRequest(int NewStatus, string? AdminNote);

    public static IEndpointRouteBuilder MapVendorDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var storefront = app.MapGroup("/api/storefront/vendors")
            .WithTags("Storefront.Vendors.Documents")
            .RequireAuthorization("CustomerOnly")
            .DisableAntiforgery();

        // Maximum upload size matches the platform media gateway (10 MB).
        // Document types accept image/* or application/pdf — anything else is
        // refused so admins don't end up reviewing zip bombs.
        const long maxBytes = 10L * 1024 * 1024;
        var allowedMime = new[] { "image/jpeg", "image/png", "image/webp", "image/heic", "application/pdf" };

        storefront.MapPost("/applications/{appId:long}/documents", async (
            long appId,
            IFormFile file,
            int documentType,
            IRepository<VendorApplication> apps,
            IRepository<VendorDocument> docs,
            IRepository<Media> medias,
            IStorageService storage,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var application = await apps.Query().FirstOrDefaultAsync(a => a.Id == appId);
            if (application is null) return Results.NotFound();
            if (application.ApplicantUserId != userId) return Results.NotFound();
            if (application.Status != VendorApplicationStatus.Pending)
            {
                return Results.BadRequest(new { error = "Application is no longer pending; documents are read-only." });
            }
            if (!Enum.IsDefined(typeof(VendorDocumentType), documentType))
            {
                return Results.BadRequest(new { error = "Invalid documentType." });
            }
            if (file is null || file.Length == 0) return Results.BadRequest(new { error = "No file uploaded." });
            if (file.Length > maxBytes) return Results.BadRequest(new { error = "File exceeds 10 MB limit." });
            if (!allowedMime.Contains(file.ContentType ?? string.Empty))
            {
                return Results.BadRequest(new { error = "Only images and PDF are accepted.", contentType = file.ContentType });
            }

            var fileName = $"vendor-doc-{appId}-{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            await using (var stream = file.OpenReadStream())
            {
                await storage.SaveMediaAsync(stream, fileName, file.ContentType);
            }
            var media = new Media
            {
                FileName = fileName,
                MediaType = MediaType.File,
                Caption = file.FileName,
            };
            medias.Add(media);
            await medias.SaveChangesAsync();

            var doc = new VendorDocument
            {
                VendorApplicationId = appId,
                MediaId = media.Id,
                DocumentType = (VendorDocumentType)documentType,
                UploadedByUserId = userId,
            };
            docs.Add(doc);
            await docs.SaveChangesAsync();

            return Results.Created($"/api/admin/vendors/applications/{appId}/documents/{doc.Id}",
                new DocumentItem(doc.Id, (int)doc.DocumentType, (int)doc.Status, doc.MediaId,
                    storage.GetMediaUrl(media.FileName), doc.CreatedOn,
                    doc.VendorApplicationId, doc.VendorId, doc.VerifiedOn, doc.AdminNote));
        });

        var admin = app.MapGroup("/api/admin/vendors")
            .WithTags("Admin.Vendors.Documents")
            .RequireAuthorization("AdminOnly");

        admin.MapGet("/applications/{appId:long}/documents", async (long appId, IRepository<VendorDocument> docs, IRepository<Media> medias, IStorageService storage) =>
        {
            var rows = await docs.Query().Include(d => d.Media)
                .Where(d => d.VendorApplicationId == appId)
                .OrderByDescending(d => d.CreatedOn)
                .ToListAsync();
            return Results.Ok(rows.Select(d => new DocumentItem(d.Id, (int)d.DocumentType, (int)d.Status, d.MediaId,
                d.Media != null ? storage.GetMediaUrl(d.Media.FileName) : string.Empty,
                d.CreatedOn, d.VendorApplicationId, d.VendorId, d.VerifiedOn, d.AdminNote)).ToList());
        });

        admin.MapGet("/{vendorId:long}/documents", async (long vendorId, IRepository<VendorDocument> docs, IRepository<Media> medias, IStorageService storage, IVendorScope scope) =>
        {
            // Wave 6 vendor scope: a vendor user can hit this for their own vendor
            // only. The admin endpoint group is AdminOnly, but the future Wave
            // 15+ vendor-facing equivalent will reuse this check.
            if (scope.CurrentVendorId is { } vid && vid != vendorId) return Results.NotFound();
            var rows = await docs.Query().Include(d => d.Media)
                .Where(d => d.VendorId == vendorId)
                .OrderByDescending(d => d.CreatedOn)
                .ToListAsync();
            return Results.Ok(rows.Select(d => new DocumentItem(d.Id, (int)d.DocumentType, (int)d.Status, d.MediaId,
                d.Media != null ? storage.GetMediaUrl(d.Media.FileName) : string.Empty,
                d.CreatedOn, d.VendorApplicationId, d.VendorId, d.VerifiedOn, d.AdminNote)).ToList());
        });

        admin.MapPatch("/documents/{docId:long}/status", async (
            long docId,
            StatusUpdateRequest req,
            IRepository<VendorDocument> docs,
            ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var adminId)) return Results.Unauthorized();
            if (!Enum.IsDefined(typeof(VendorDocumentStatus), req.NewStatus))
            {
                return Results.BadRequest(new { error = "Invalid status." });
            }
            var doc = await docs.Query().FirstOrDefaultAsync(d => d.Id == docId);
            if (doc is null) return Results.NotFound();
            var next = (VendorDocumentStatus)req.NewStatus;
            doc.Status = next;
            doc.VerifiedByUserId = adminId;
            doc.VerifiedOn = DateTimeOffset.UtcNow;
            doc.AdminNote = req.AdminNote ?? string.Empty;
            await docs.SaveChangesAsync();
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

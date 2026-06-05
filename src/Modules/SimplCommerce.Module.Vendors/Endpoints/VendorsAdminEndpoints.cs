#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Vendors.Endpoints;

public static class VendorsAdminEndpoints
{
    public record VendorInput(string Name, string Slug, string? Description, string? Email, bool IsActive);
    public record VendorDetail(long Id, string Name, string Slug, string? Description, string? Email, bool IsActive, System.DateTimeOffset CreatedOn);

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

        group.MapGet("/{id:long}", async (long id, IRepository<Vendor> repo) =>
        {
            var v = await repo.Query().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return v is null
                ? Results.NotFound()
                : Results.Ok(new VendorDetail(v.Id, v.Name, v.Slug, v.Description, v.Email, v.IsActive, v.CreatedOn));
        });

        group.MapPost("/", async (VendorInput input, IRepository<Vendor> repo) =>
        {
            var vendor = new Vendor
            {
                Name = input.Name,
                Slug = input.Slug,
                Description = input.Description ?? string.Empty,
                Email = input.Email ?? string.Empty,
                IsActive = input.IsActive,
            };
            repo.Add(vendor);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/vendors/{vendor.Id}", new { vendor.Id });
        });

        group.MapPut("/{id:long}", async (long id, VendorInput input, IRepository<Vendor> repo) =>
        {
            var vendor = await repo.Query().FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (vendor is null) return Results.NotFound();
            vendor.Name = input.Name;
            vendor.Slug = input.Slug;
            vendor.Description = input.Description ?? string.Empty;
            vendor.Email = input.Email ?? string.Empty;
            vendor.IsActive = input.IsActive;
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

        return app;
    }
}

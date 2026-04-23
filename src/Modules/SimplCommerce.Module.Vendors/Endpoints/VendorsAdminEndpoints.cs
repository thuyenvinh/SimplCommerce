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
    public record VendorInput(string Name, string Slug, string? Description);

    public static IEndpointRouteBuilder MapVendorsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/vendors")
            .WithTags("Admin.Vendors")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IRepository<Vendor> repo) =>
        {
            var list = await repo.Query().Where(v => !v.IsDeleted)
                .Select(v => new { v.Id, v.Name, v.Slug, v.Description })
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapPost("/", (VendorInput input, IRepository<Vendor> repo) =>
        {
            var vendor = new Vendor { Name = input.Name, Slug = input.Slug, Description = input.Description ?? string.Empty };
            repo.Add(vendor);
            repo.SaveChanges();
            return Results.Created($"/api/admin/vendors/{vendor.Id}", new { vendor.Id });
        });

        return app;
    }
}

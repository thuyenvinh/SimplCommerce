#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Shipping.Models;

namespace SimplCommerce.Module.Shipping.Endpoints;

public static class ShippingAdminEndpoints
{
    public static IEndpointRouteBuilder MapShippingAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/shipping")
            .WithTags("Admin.Shipping")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/providers", async (IRepositoryWithTypedId<ShippingProvider, string> repo) =>
            Results.Ok(await repo.Query().Select(p => new { p.Id, p.Name, p.IsEnabled }).ToListAsync()));

        return app;
    }
}

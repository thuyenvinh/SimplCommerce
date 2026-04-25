#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Payments.Models;

namespace SimplCommerce.Module.Payments.Endpoints;

public static class PaymentsAdminEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/payments")
            .WithTags("Admin.Payments")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/providers", async (IRepositoryWithTypedId<PaymentProvider, string> repo) =>
            Results.Ok(await repo.Query()
                .Select(p => new { p.Id, p.Name, p.IsEnabled })
                .ToListAsync()));

        group.MapGet("/", async (IRepository<Payment> repo, int page = 1, int pageSize = 20) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var total = await repo.Query().CountAsync();
            var rows = await repo.Query().OrderByDescending(p => p.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new { p.Id, p.OrderId, p.PaymentMethod, p.PaymentFee, p.Amount, p.Status, p.CreatedOn })
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        return app;
    }
}

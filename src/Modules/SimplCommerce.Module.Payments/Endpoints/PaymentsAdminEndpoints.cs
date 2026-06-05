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
    public record PaymentProviderItem(string Id, string Name, bool IsEnabled);
    public record PaymentProviderDetail(string Id, string Name, bool IsEnabled, string? AdditionalSettings);
    public record PaymentProviderInput(bool IsEnabled, string? AdditionalSettings);

    public static IEndpointRouteBuilder MapPaymentsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/payments")
            .WithTags("Admin.Payments")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/providers", async (IRepositoryWithTypedId<PaymentProvider, string> repo) =>
            Results.Ok(await repo.Query()
                .OrderBy(p => p.Name)
                .Select(p => new PaymentProviderItem(p.Id, p.Name, p.IsEnabled))
                .ToListAsync()));

        group.MapGet("/providers/{id}", async (string id, IRepositoryWithTypedId<PaymentProvider, string> repo) =>
        {
            var p = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            return p is null
                ? Results.NotFound()
                : Results.Ok(new PaymentProviderDetail(p.Id, p.Name, p.IsEnabled, p.AdditionalSettings));
        });

        group.MapPut("/providers/{id}", async (string id, PaymentProviderInput input, IRepositoryWithTypedId<PaymentProvider, string> repo) =>
        {
            var p = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return Results.NotFound();
            p.IsEnabled = input.IsEnabled;
            p.AdditionalSettings = input.AdditionalSettings ?? string.Empty;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

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

#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Tax.Models;

namespace SimplCommerce.Module.Tax.Endpoints;

public static class TaxAdminEndpoints
{
    public record TaxClassInput(string Name);
    public record TaxRateInput(long TaxClassId, string CountryId, long? StateOrProvinceId, string? ZipCode, decimal Rate);

    public static IEndpointRouteBuilder MapTaxAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/tax")
            .WithTags("Admin.Tax")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/classes", async (IRepository<TaxClass> repo) =>
            Results.Ok(await repo.Query().Select(c => new { c.Id, c.Name }).ToListAsync()));

        group.MapPost("/classes", (TaxClassInput input, IRepository<TaxClass> repo) =>
        {
            var tc = new TaxClass { Name = input.Name };
            repo.Add(tc);
            repo.SaveChanges();
            return Results.Created($"/api/admin/tax/classes/{tc.Id}", new { tc.Id });
        });

        group.MapGet("/rates", async (IRepository<TaxRate> repo) =>
            Results.Ok(await repo.Query()
                .Select(r => new { r.Id, r.TaxClassId, r.CountryId, r.StateOrProvinceId, r.ZipCode, r.Rate })
                .ToListAsync()));

        group.MapPost("/rates", (TaxRateInput input, IRepository<TaxRate> repo) =>
        {
            var rate = new TaxRate
            {
                TaxClassId = input.TaxClassId,
                CountryId = input.CountryId,
                StateOrProvinceId = input.StateOrProvinceId,
                ZipCode = input.ZipCode ?? string.Empty,
                Rate = input.Rate,
            };
            repo.Add(rate);
            repo.SaveChanges();
            return Results.Created($"/api/admin/tax/rates/{rate.Id}", new { rate.Id });
        });

        return app;
    }
}

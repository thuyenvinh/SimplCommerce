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
    public record ShippingProviderItem(string Id, string Name, bool IsEnabled);
    public record ShippingProviderDetail(string Id, string Name, bool IsEnabled,
        bool ToAllShippingEnabledCountries, string? OnlyCountryIdsString,
        bool ToAllShippingEnabledStatesOrProvinces, string? OnlyStateOrProvinceIdsString,
        string? AdditionalSettings);
    public record ShippingProviderInput(bool IsEnabled,
        bool ToAllShippingEnabledCountries, string? OnlyCountryIdsString,
        bool ToAllShippingEnabledStatesOrProvinces, string? OnlyStateOrProvinceIdsString,
        string? AdditionalSettings);

    public static IEndpointRouteBuilder MapShippingAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/shipping")
            .WithTags("Admin.Shipping")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/providers", async (IRepositoryWithTypedId<ShippingProvider, string> repo) =>
            Results.Ok(await repo.Query()
                .OrderBy(p => p.Name)
                .Select(p => new ShippingProviderItem(p.Id, p.Name, p.IsEnabled))
                .ToListAsync()));

        group.MapGet("/providers/{id}", async (string id, IRepositoryWithTypedId<ShippingProvider, string> repo) =>
        {
            var p = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            return p is null
                ? Results.NotFound()
                : Results.Ok(new ShippingProviderDetail(p.Id, p.Name, p.IsEnabled,
                    p.ToAllShippingEnabledCountries, p.OnlyCountryIdsString,
                    p.ToAllShippingEnabledStatesOrProvinces, p.OnlyStateOrProvinceIdsString,
                    p.AdditionalSettings));
        });

        group.MapPut("/providers/{id}", async (string id, ShippingProviderInput input, IRepositoryWithTypedId<ShippingProvider, string> repo) =>
        {
            var p = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return Results.NotFound();
            p.IsEnabled = input.IsEnabled;
            p.ToAllShippingEnabledCountries = input.ToAllShippingEnabledCountries;
            p.OnlyCountryIdsString = input.OnlyCountryIdsString ?? string.Empty;
            p.ToAllShippingEnabledStatesOrProvinces = input.ToAllShippingEnabledStatesOrProvinces;
            p.OnlyStateOrProvinceIdsString = input.OnlyStateOrProvinceIdsString ?? string.Empty;
            p.AdditionalSettings = input.AdditionalSettings ?? string.Empty;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

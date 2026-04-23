#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Core.Endpoints;

public static class CoreStorefrontEndpoints
{
    public record CountryItem(string Id, string Name, string Code3, bool IsBillingEnabled, bool IsShippingEnabled);
    public record StateOrProvinceItem(long Id, string Name, string CountryId);
    public record AddressDto(long Id, string ContactName, string Phone, string AddressLine1, string City, string ZipCode);

    public static IEndpointRouteBuilder MapCoreStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/core").WithTags("Storefront.Core");

        group.MapGet("/countries", async (IRepositoryWithTypedId<Country, string> repo) =>
        {
            var list = await repo.Query()
                .OrderBy(c => c.Name)
                .Select(c => new CountryItem(c.Id, c.Name, c.Code3, c.IsBillingEnabled, c.IsShippingEnabled))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/countries/{countryId}/states", async (string countryId, IRepository<StateOrProvince> repo) =>
        {
            var list = await repo.Query()
                .Where(s => s.CountryId == countryId)
                .OrderBy(s => s.Name)
                .Select(s => new StateOrProvinceItem(s.Id, s.Name, s.CountryId))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/addresses", async (IRepository<UserAddress> repo, ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var list = await repo.Query()
                .Where(a => a.UserId == userId)
                .Select(a => new AddressDto(a.Address.Id, a.Address.ContactName, a.Address.Phone,
                    a.Address.AddressLine1, a.Address.City, a.Address.ZipCode))
                .ToListAsync();
            return Results.Ok(list);
        }).RequireAuthorization("CustomerOnly");

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId)
    {
        userId = 0;
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return long.TryParse(raw, out userId);
    }
}

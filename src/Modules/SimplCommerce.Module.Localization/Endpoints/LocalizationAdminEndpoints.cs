#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Localization.Endpoints;

public static class LocalizationAdminEndpoints
{
    public record CultureInput(string Id, string Name);

    public static IEndpointRouteBuilder MapLocalizationAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/localization")
            .WithTags("Admin.Localization")
            .RequireAuthorization("AdminOnly");

        // Languages and resources live in Core.AppSetting / culture system — minimal
        // read-only surface here; full CRUD is a follow-up once translation editor UI
        // lands in Phase 5.
        group.MapGet("/app-settings", async (IRepositoryWithTypedId<AppSetting, string> repo) =>
            Results.Ok(await repo.Query().Select(s => new { s.Id, s.Value, s.Module, s.IsVisibleInCommonSettingPage }).ToListAsync()));

        return app;
    }
}

#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Infrastructure.Localization;

namespace SimplCommerce.Module.Localization.Endpoints;

/// <summary>
/// Admin CRUD for cultures (languages) + translation resources.
/// Each resource is (CultureId, Key) -> Value. Looking up a translation
/// at runtime walks <c>EFStringLocalizer</c> which reads the same table.
/// </summary>
public static class LocalizationAdminEndpoints
{
    public record CultureInput(string Id, string Name);
    public record CultureItem(string Id, string Name, int ResourceCount);
    public record ResourceInput(string CultureId, string Key, string? Value);
    public record ResourceItem(long Id, string CultureId, string Key, string? Value);

    public static IEndpointRouteBuilder MapLocalizationAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/localization")
            .WithTags("Admin.Localization")
            .RequireAuthorization("AdminOnly");

        // --- Cultures ---
        group.MapGet("/cultures", async (IRepositoryWithTypedId<Culture, string> repo, IRepository<Resource> resources) =>
        {
            var cultures = await repo.Query().OrderBy(c => c.Id).Select(c => new { c.Id, c.Name }).ToListAsync();
            var counts = await resources.Query()
                .GroupBy(r => r.CultureId)
                .Select(g => new { CultureId = g.Key, Count = g.Count() })
                .ToListAsync();
            var dict = counts.ToDictionary(x => x.CultureId, x => x.Count);
            var list = cultures.Select(c => new CultureItem(c.Id, c.Name, dict.TryGetValue(c.Id, out var n) ? n : 0)).ToList();
            return Results.Ok(list);
        });

        group.MapPost("/cultures", async (CultureInput input, IRepositoryWithTypedId<Culture, string> repo) =>
        {
            if (string.IsNullOrWhiteSpace(input.Id)) return Results.BadRequest(new { error = "Id is required (e.g. vi-VN)." });
            if (await repo.Query().AnyAsync(c => c.Id == input.Id)) return Results.Conflict(new { error = "Culture already exists." });
            var culture = new Culture(input.Id) { Name = input.Name };
            repo.Add(culture);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/localization/cultures/{culture.Id}", new { culture.Id });
        });

        group.MapDelete("/cultures/{id}", async (string id, IRepositoryWithTypedId<Culture, string> repo) =>
        {
            var culture = await repo.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (culture is null) return Results.NotFound();
            repo.Remove(culture);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        // --- Resources ---
        group.MapGet("/resources", async (IRepository<Resource> repo, string? cultureId = null, string? search = null, int page = 1, int pageSize = 100) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 500);
            var query = repo.Query();
            if (!string.IsNullOrWhiteSpace(cultureId)) query = query.Where(r => r.CultureId == cultureId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(r => EF.Functions.Like(r.Key, pattern) || EF.Functions.Like(r.Value ?? string.Empty, pattern));
            }
            var total = await query.CountAsync();
            var items = await query.OrderBy(r => r.Key)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new ResourceItem(r.Id, r.CultureId, r.Key, r.Value))
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items });
        });

        group.MapPost("/resources", async (ResourceInput input, IRepository<Resource> repo) =>
        {
            if (string.IsNullOrWhiteSpace(input.Key) || string.IsNullOrWhiteSpace(input.CultureId))
            {
                return Results.BadRequest(new { error = "Key and CultureId are required." });
            }
            var existing = await repo.Query().FirstOrDefaultAsync(r => r.CultureId == input.CultureId && r.Key == input.Key);
            if (existing is not null)
            {
                existing.Value = input.Value ?? string.Empty;
                await repo.SaveChangesAsync();
                return Results.Ok(new { existing.Id });
            }
            var res = new Resource { CultureId = input.CultureId, Key = input.Key, Value = input.Value ?? string.Empty };
            repo.Add(res);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/localization/resources/{res.Id}", new { res.Id });
        });

        group.MapPut("/resources/{id:long}", async (long id, ResourceInput input, IRepository<Resource> repo) =>
        {
            var res = await repo.Query().FirstOrDefaultAsync(r => r.Id == id);
            if (res is null) return Results.NotFound();
            res.CultureId = input.CultureId;
            res.Key = input.Key;
            res.Value = input.Value ?? string.Empty;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/resources/{id:long}", async (long id, IRepository<Resource> repo) =>
        {
            var res = await repo.Query().FirstOrDefaultAsync(r => r.Id == id);
            if (res is null) return Results.NotFound();
            repo.Remove(res);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

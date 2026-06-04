#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Core.Endpoints;

public static class CoreAdminEndpoints
{
    public record CreateUserRequest(string Email, string FullName, string Password, string? Role);
    public record AppSettingItem(string Id, string Value, string? Module);
    public record AppSettingInput(string Value);
    public record CustomerGroupItem(long Id, string Name);
    public record CustomerGroupInput(string Name);

    public static IEndpointRouteBuilder MapCoreAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/core")
            .WithTags("Admin.Core")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/users", async (UserManager<User> userManager, int page = 1, int pageSize = 20, string? search = null) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var query = userManager.Users.Where(u => !u.IsDeleted);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(u => EF.Functions.Like(u.Email ?? string.Empty, pattern) || EF.Functions.Like(u.FullName ?? string.Empty, pattern));
            }
            var total = await query.CountAsync();
            var rows = await query.OrderByDescending(u => u.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(u => new { u.Id, u.Email, u.FullName, u.CreatedOn, u.LockoutEnabled })
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        group.MapGet("/users/{id:long}", async (long id, UserManager<User> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user is null || user.IsDeleted) return Results.NotFound();
            var roles = await userManager.GetRolesAsync(user);
            return Results.Ok(new { user.Id, user.Email, user.FullName, user.CreatedOn, Roles = roles });
        });

        group.MapPost("/users", async (CreateUserRequest req, UserManager<User> userManager) =>
        {
            var user = new User
            {
                UserName = req.Email, Email = req.Email, FullName = req.FullName,
                NormalizedUserName = req.Email.ToUpperInvariant(),
                NormalizedEmail = req.Email.ToUpperInvariant(),
            };
            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded) return Results.BadRequest(result.Errors);
            if (!string.IsNullOrWhiteSpace(req.Role))
            {
                await userManager.AddToRoleAsync(user, req.Role);
            }
            return Results.Created($"/api/admin/core/users/{user.Id}", new { user.Id });
        });

        group.MapGet("/roles", async (RoleManager<Role> roleManager) =>
        {
            var roles = await roleManager.Roles.Select(r => new { r.Id, r.Name }).ToListAsync();
            return Results.Ok(roles);
        });

        group.MapGet("/countries", async (IRepositoryWithTypedId<Country, string> repo) =>
        {
            var list = await repo.Query().OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.Code3, c.IsBillingEnabled, c.IsShippingEnabled })
                .ToListAsync();
            return Results.Ok(list);
        });

        // --- App settings (per-key string store) ---
        group.MapGet("/app-settings", async (IRepositoryWithTypedId<AppSetting, string> repo, string? module = null) =>
        {
            var query = repo.Query();
            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(s => s.Module == module);
            }
            var list = await query.OrderBy(s => s.Id)
                .Select(s => new AppSettingItem(s.Id, s.Value, s.Module))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapPut("/app-settings/{id}", async (string id, AppSettingInput input, IRepositoryWithTypedId<AppSetting, string> repo) =>
        {
            var setting = await repo.Query().FirstOrDefaultAsync(s => s.Id == id);
            if (setting is null)
            {
                setting = new AppSetting(id) { Value = input.Value };
                repo.Add(setting);
            }
            else
            {
                setting.Value = input.Value;
            }
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        // --- Customer groups ---
        group.MapGet("/customer-groups", async (IRepository<CustomerGroup> repo) =>
        {
            var list = await repo.Query().OrderBy(g => g.Name)
                .Select(g => new CustomerGroupItem(g.Id, g.Name))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapPost("/customer-groups", async (CustomerGroupInput input, IRepository<CustomerGroup> repo) =>
        {
            if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest(new { error = "Name is required." });
            var group = new CustomerGroup { Name = input.Name };
            repo.Add(group);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/core/customer-groups/{group.Id}", new { group.Id });
        });

        group.MapPut("/customer-groups/{id:long}", async (long id, CustomerGroupInput input, IRepository<CustomerGroup> repo) =>
        {
            var group = await repo.Query().FirstOrDefaultAsync(g => g.Id == id);
            if (group is null) return Results.NotFound();
            group.Name = input.Name;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/customer-groups/{id:long}", async (long id, IRepository<CustomerGroup> repo) =>
        {
            var group = await repo.Query().FirstOrDefaultAsync(g => g.Id == id);
            if (group is null) return Results.NotFound();
            repo.Remove(group);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}

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
                query = query.Where(u => EF.Functions.Like(u.Email, pattern) || EF.Functions.Like(u.FullName, pattern));
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

        return app;
    }
}

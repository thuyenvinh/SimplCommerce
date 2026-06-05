#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.ActivityLog.Models;

namespace SimplCommerce.Module.ActivityLog.Endpoints;

public static class ActivityLogAdminEndpoints
{
    public static IEndpointRouteBuilder MapActivityLogAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/activity-log")
            .WithTags("Admin.ActivityLog")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IRepository<Activity> repo, int page = 1, int pageSize = 50) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var total = await repo.Query().CountAsync();
            var rows = await repo.Query()
                .OrderByDescending(a => a.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new { a.Id, a.ActivityTypeId, a.UserId, a.EntityId, a.EntityTypeId, a.CreatedOn })
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        return app;
    }
}

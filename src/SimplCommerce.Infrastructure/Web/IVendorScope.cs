#nullable enable
using System.Security.Claims;

namespace SimplCommerce.Infrastructure.Web;

/// <summary>
/// Wave 6: vendor-aware request scope. The JWT carries a <c>vendor_id</c> claim when
/// the authenticated user is a vendor (User.VendorId is set at registration / admin
/// onboarding); this helper surfaces it to admin endpoints so they can filter
/// repositories down to the vendor's own data.
///
/// Semantics:
/// • <see cref="CurrentVendorId"/> non-null → caller is a vendor; endpoints MUST
///   filter <c>WHERE entity.VendorId == value</c> on every query and 404 / 403 on
///   any single-row lookup that targets a different vendor's row.
/// • null → caller is a platform admin (or not authenticated); no vendor filter
///   applies, role-based authorization at the endpoint still gates access.
///
/// The principal is read from <see cref="IHttpContextAccessor"/> rather than injected
/// per-handler so a single .Use(...) per endpoint stays terse. Tests can stub the
/// interface directly.
/// </summary>
public interface IVendorScope
{
    long? CurrentVendorId { get; }
    bool IsVendor => CurrentVendorId.HasValue;
}

public sealed class VendorScope : IVendorScope
{
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _accessor;
    private long? _cached;
    private bool _resolved;

    public VendorScope(Microsoft.AspNetCore.Http.IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public long? CurrentVendorId
    {
        get
        {
            if (_resolved) return _cached;
            _resolved = true;
            var principal = _accessor.HttpContext?.User;
            var raw = principal?.FindFirst("vendor_id")?.Value;
            if (long.TryParse(raw, out var v)) _cached = v;
            return _cached;
        }
    }
}

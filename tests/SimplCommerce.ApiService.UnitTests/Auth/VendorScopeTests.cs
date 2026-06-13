using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SimplCommerce.Infrastructure.Web;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Auth;

// Wave 6: VendorScope is the single point where every admin endpoint reads
// "am I a vendor right now?". Mis-parsing the claim would either leak data
// across vendors (false negative) or block legitimate admin access (false
// positive), so the parse logic gets its own pinned tests even though it's
// only a handful of lines.
public class VendorScopeTests
{
    private static IVendorScope Build(params Claim[] claims)
    {
        var ctx = new DefaultHttpContext();
        if (claims.Length > 0)
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(ctx);
        return new VendorScope(accessor.Object);
    }

    [Fact]
    public void Returns_null_when_no_claim_present()
    {
        var scope = Build();
        scope.CurrentVendorId.Should().BeNull();
        scope.IsVendor.Should().BeFalse();
    }

    [Fact]
    public void Returns_null_when_principal_has_no_vendor_id_claim()
    {
        var scope = Build(new Claim(ClaimTypes.NameIdentifier, "42"));
        scope.CurrentVendorId.Should().BeNull();
    }

    [Fact]
    public void Returns_vendor_id_when_claim_present()
    {
        var scope = Build(new Claim("vendor_id", "123"));
        scope.CurrentVendorId.Should().Be(123L);
        scope.IsVendor.Should().BeTrue();
    }

    [Fact]
    public void Garbage_claim_value_degrades_to_null_not_throw()
    {
        // Defensive: a future tampered / mis-issued token shouldn't crash the
        // endpoint — we treat unparseable as "not a vendor" which means admin
        // can still investigate the user via the platform-admin surface.
        var scope = Build(new Claim("vendor_id", "not-a-long"));
        scope.CurrentVendorId.Should().BeNull();
    }

    [Fact]
    public void Caches_resolution_per_instance()
    {
        // Scoped service: parsed once per request. Calling twice mustn't re-walk
        // the claim list (small perf win, but mostly to keep the contract clear).
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("vendor_id", "7") }, "TestAuth"));
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(ctx);
        var scope = new VendorScope(accessor.Object);

        scope.CurrentVendorId.Should().Be(7L);
        scope.CurrentVendorId.Should().Be(7L);
        // Accessor only walked once (HttpContext is fetched on first read).
        accessor.Verify(a => a.HttpContext, Times.Once);
    }
}

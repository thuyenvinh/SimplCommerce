using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SimplCommerce.ApiService.Auth;
using SimplCommerce.Module.Core.Models;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Auth;

/// <summary>
/// Exercises <see cref="JwtTokenService"/> against a mocked UserManager so the EF
/// model-configuration modules (ICustomModelBuilder scan) don't need to participate.
/// </summary>
public class JwtTokenServiceTests
{
    private static Mock<UserManager<User>> MockUserManager(string[]? roles = null)
    {
        var store = new Mock<IUserStore<User>>();
        var mgr = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(roles ?? Array.Empty<string>());
        return mgr;
    }

    private static IConfiguration Config(Action<Dictionary<string, string?>>? overrides = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"]                   = "simpl-test",
            ["Jwt:Audience"]                 = "simpl-test-clients",
            ["Jwt:SigningKey"]               = "unit-test-signing-key-32-bytes-minimum!",
            ["Jwt:AccessTokenLifetimeHours"] = "1",
        };
        overrides?.Invoke(dict);
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static User SampleUser() => new()
    {
        Id = 42, UserName = "a@b.com", Email = "a@b.com", FullName = "Alice Tester",
    };

    [Fact]
    public async Task IssueAsync_returns_token_that_validates_against_same_params()
    {
        var cfg = Config();
        var sut = new JwtTokenService(cfg, MockUserManager().Object);

        var resp = await sut.IssueAsync(SampleUser());

        resp.AccessToken.Should().NotBeNullOrWhiteSpace();
        resp.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = cfg["Jwt:Issuer"],
            ValidAudience = cfg["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:SigningKey"]!)),
        };
        var principal = handler.ValidateToken(resp.AccessToken, validation, out _);
        principal.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task Token_includes_sub_email_name_and_jti_claims()
    {
        var sut = new JwtTokenService(Config(), MockUserManager().Object);
        var user = SampleUser();

        var resp = await sut.IssueAsync(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resp.AccessToken);

        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name && c.Value == user.FullName);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public async Task Default_lifetime_is_eight_hours_when_config_missing()
    {
        var cfg = Config(d => d.Remove("Jwt:AccessTokenLifetimeHours"));
        var sut = new JwtTokenService(cfg, MockUserManager().Object);

        var resp = await sut.IssueAsync(SampleUser());

        // Default is 8h; allow a minute of slack for test execution time.
        resp.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(8), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Configured_lifetime_is_honoured()
    {
        var cfg = Config(d => d["Jwt:AccessTokenLifetimeHours"] = "2");
        var sut = new JwtTokenService(cfg, MockUserManager().Object);

        var resp = await sut.IssueAsync(SampleUser());

        resp.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(2), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Roles_are_surfaced_on_the_response_and_embedded_as_claims()
    {
        var sut = new JwtTokenService(Config(), MockUserManager(roles: new[] { "admin", "vendor" }).Object);

        var resp = await sut.IssueAsync(SampleUser());

        resp.Roles.Should().BeEquivalentTo("admin", "vendor");

        var token = new JwtSecurityTokenHandler().ReadJwtToken(resp.AccessToken);
        token.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value).Should().BeEquivalentTo("admin", "vendor");
    }

    [Fact]
    public async Task Every_token_gets_a_fresh_jti()
    {
        var sut = new JwtTokenService(Config(), MockUserManager().Object);

        var a = await sut.IssueAsync(SampleUser());
        var b = await sut.IssueAsync(SampleUser());

        var jtiA = new JwtSecurityTokenHandler().ReadJwtToken(a.AccessToken)
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jtiB = new JwtSecurityTokenHandler().ReadJwtToken(b.AccessToken)
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jtiA.Should().NotBe(jtiB);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.ApiService.Auth;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = "simplcommerce-api";
    public string Audience { get; init; } = "simplcommerce-clients";
    public string SigningKey { get; init; } = "dev-only-change-me-32-bytes-minimum!!";
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromHours(8);
}

/// <summary>
/// Thin wrapper around <see cref="JwtSecurityTokenHandler"/> that shares the same
/// validation parameters as the JwtBearer middleware registered in Program.cs. Keep
/// signing-key / issuer / audience in one place so token-minting and token-validating
/// stay consistent.
/// </summary>
public sealed class JwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly UserManager<User> _userManager;

    public JwtTokenService(IConfiguration configuration, UserManager<User> userManager)
    {
        _userManager = userManager;
        _settings = new JwtSettings
        {
            Issuer = configuration["Jwt:Issuer"] ?? "simplcommerce-api",
            Audience = configuration["Jwt:Audience"] ?? "simplcommerce-clients",
            SigningKey = configuration["Jwt:SigningKey"] ?? "dev-only-change-me-32-bytes-minimum!!",
            AccessTokenLifetime = TimeSpan.FromHours(
                configuration.GetValue("Jwt:AccessTokenLifetimeHours", 8))
        };
    }

    public async Task<LoginResponse> IssueAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var expiresAt = DateTimeOffset.UtcNow.Add(_settings.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName ?? user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new LoginResponse(encoded, expiresAt, [.. roles]);
    }
}

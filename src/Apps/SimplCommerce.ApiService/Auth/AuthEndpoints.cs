using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.ApiService.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth").RequireRateLimiting("auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync).RequireAuthorization();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapPost("/forgot-password", ForgotPasswordAsync);
        group.MapPost("/reset-password", ResetPasswordAsync);
        group.MapGet("/me", MeAsync).RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok<LoginResponse>, ValidationProblem, Conflict<string>>> RegisterAsync(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        UserManager<User> userManager,
        JwtTokenService tokens)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return TypedResults.Conflict("Email already registered.");
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            NormalizedUserName = request.Email.ToUpperInvariant(),
            NormalizedEmail = request.Email.ToUpperInvariant(),
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return TypedResults.ValidationProblem(result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        return TypedResults.Ok(await tokens.IssueAsync(user));
    }

    private static async Task<Results<Ok<LoginResponse>, ValidationProblem, UnauthorizedHttpResult>> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        UserManager<User> userManager,
        JwtTokenService tokens)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await tokens.IssueAsync(user));
    }

    private static async Task<Results<Ok<LoginResponse>, UnauthorizedHttpResult>> RefreshAsync(
        ClaimsPrincipal principal,
        UserManager<User> userManager,
        JwtTokenService tokens)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (!long.TryParse(userId, out var id)) return TypedResults.Unauthorized();

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return TypedResults.Unauthorized();

        return TypedResults.Ok(await tokens.IssueAsync(user));
    }

    private static NoContent LogoutAsync()
    {
        // JWT is stateless; token invalidation is a deferred concern (revocation list,
        // short TTL + refresh token). The endpoint exists so BFF clients can treat
        // it uniformly.
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        IValidator<ForgotPasswordRequest> validator,
        UserManager<User> userManager,
        IEmailSender emailSender)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid) return TypedResults.NoContent(); // don't leak existence

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return TypedResults.NoContent();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        // Real email template wiring lives in Module.EmailSenderSmtp; this keeps the wire
        // by sending a minimal message. The Storefront (Phase 4) renders the reset link
        // using its own routes and includes the token + email in the URL.
        await emailSender.SendEmailAsync(user.Email!, "Reset your password",
            $"Use this token to reset your password: {token}");
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ValidationProblem>> ResetPasswordAsync(
        ResetPasswordRequest request,
        IValidator<ResetPasswordRequest> validator,
        UserManager<User> userManager)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return TypedResults.NoContent();

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return TypedResults.ValidationProblem(result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> MeAsync(
        ClaimsPrincipal principal,
        UserManager<User> userManager)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (!long.TryParse(userId, out var id)) return TypedResults.Unauthorized();

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return TypedResults.Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        return TypedResults.Ok(new MeResponse(user.Id, user.Email ?? string.Empty,
            user.FullName ?? string.Empty, [.. roles]));
    }
}

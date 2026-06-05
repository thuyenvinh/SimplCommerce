namespace SimplCommerce.ApiService.Auth;

/// <summary>
/// Request/response contracts for JWT auth endpoints. These stay inside the API service
/// because they are bound to the JWT token shape, not the module-level domain contracts.
/// </summary>
public sealed record RegisterRequest(string Email, string Password, string FullName);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string[] Roles);

public sealed record RefreshRequest(string AccessToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record MeResponse(long UserId, string Email, string FullName, string[] Roles);

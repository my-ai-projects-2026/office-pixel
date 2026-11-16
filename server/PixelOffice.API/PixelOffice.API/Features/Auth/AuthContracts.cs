namespace PixelOffice.API.Features.Auth;

// Request and response shapes for /api/auth (plan.md, API). Mirrored by the client's contracts.ts (T24).

public sealed record RegisterRequest(string? Email, string? Password, string? DisplayName);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record PlayerProfile(Guid Id, string DisplayName, int AvatarId);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, PlayerProfile Player);

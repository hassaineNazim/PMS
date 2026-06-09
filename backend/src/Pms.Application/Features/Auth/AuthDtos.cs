using Pms.Domain.Enums;

namespace Pms.Application.Features.Auth;

public record LoginRequest(string Email, string Password, string? TenantSlug);

public record AuthResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    UserDto User);

public record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
    string FullName,
    UserRole Role);

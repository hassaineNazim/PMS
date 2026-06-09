using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Pms.Application.Common;
using Pms.Domain.Enums;

namespace Pms.Api.Security;

/// <summary>Reads the authenticated principal from the current HTTP request.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : null;

    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var r) ? r : null;
}

using Pms.Domain.Enums;

namespace Pms.Application.Common;

/// <summary>The authenticated user behind the current request, if any.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}

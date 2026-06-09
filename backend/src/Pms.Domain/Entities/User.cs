using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

/// <summary>
/// A login account scoped to a tenant. Distinct from <see cref="Staff"/> (HR
/// records). Authentication is by email + bcrypt password hash; authorization
/// by <see cref="UserRole"/>.
/// </summary>
public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Receptionist;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
}

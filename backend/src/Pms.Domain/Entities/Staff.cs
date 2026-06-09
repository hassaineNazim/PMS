using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

/// <summary>HR record for an employee. Not a login account (see <see cref="User"/>).</summary>
public class Staff : TenantEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public StaffRole Role { get; set; } = StaffRole.Other;
    public string? Department { get; set; }
    public DateOnly HireDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public StaffStatus Status { get; set; } = StaffStatus.Active;

    public List<StaffSchedule> Schedules { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();
}

using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

/// <summary>
/// Per-tenant commercial license / activation. Enforces plan limits (e.g. max
/// rooms) and an expiry date. A middleware rejects requests for tenants whose
/// license is missing, inactive or expired.
/// </summary>
public class License : Entity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    /// <summary>Opaque activation key handed to the customer.</summary>
    public string Key { get; set; } = string.Empty;

    public LicensePlan Plan { get; set; } = LicensePlan.Trial;

    /// <summary>0 = unlimited.</summary>
    public int MaxRooms { get; set; }

    /// <summary>0 = unlimited.</summary>
    public int MaxUsers { get; set; }

    public DateTimeOffset ValidFrom { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ValidUntil { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
    public bool IsActive { get; set; } = true;

    public bool IsValid(DateTimeOffset now) => IsActive && now >= ValidFrom && now <= ValidUntil;
}

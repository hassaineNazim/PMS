using Pms.Domain.Common;

namespace Pms.Domain.Entities;

/// <summary>
/// Append-only record of significant actions (check-in, IPTV push, invoice
/// creation…). Used for traceability and debugging integrations in the field.
/// </summary>
public class AuditLog : TenantEntity
{
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }

    /// <summary>Arbitrary JSON payload (stored as jsonb).</summary>
    public string? Details { get; set; }

    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
}

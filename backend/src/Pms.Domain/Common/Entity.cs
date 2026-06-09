namespace Pms.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Uses a GUID primary key (safe for
/// multi-tenant data, distributed inserts and avoids leaking row counts).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

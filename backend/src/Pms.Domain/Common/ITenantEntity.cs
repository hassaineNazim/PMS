namespace Pms.Domain.Common;

/// <summary>
/// Marks an entity as belonging to a tenant (establishment). The infrastructure
/// layer applies a global query filter on <see cref="TenantId"/> so a tenant can
/// never read or write another tenant's rows. This is the cornerstone of the
/// multi-tenant data isolation.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

/// <summary>Base class for tenant-scoped entities.</summary>
public abstract class TenantEntity : Entity, ITenantEntity
{
    public Guid TenantId { get; set; }
}

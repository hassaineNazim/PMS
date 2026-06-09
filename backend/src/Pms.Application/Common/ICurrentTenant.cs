namespace Pms.Application.Common;

/// <summary>
/// Ambient information about the tenant the current request belongs to. Resolved
/// by middleware (from the JWT or a header) and consumed by the DbContext global
/// query filter and by services that stamp new rows.
/// </summary>
public interface ICurrentTenant
{
    Guid TenantId { get; }
    bool HasTenant { get; }

    /// <summary>Temporarily override the tenant (used by background jobs / seeding).</summary>
    IDisposable BeginScope(Guid tenantId);
}

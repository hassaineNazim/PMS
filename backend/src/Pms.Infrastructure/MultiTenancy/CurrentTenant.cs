using Pms.Application.Common;

namespace Pms.Infrastructure.MultiTenancy;

/// <summary>
/// Scoped holder of the current request's tenant. Set by the tenant-resolution
/// middleware (from the JWT/header). <see cref="BeginScope"/> provides a temporary
/// override (used by the seeder and background jobs that run without an HTTP request).
/// </summary>
public sealed class CurrentTenant : ICurrentTenant
{
    private static readonly AsyncLocal<Guid?> ScopeOverride = new();
    private Guid _tenantId;

    public Guid TenantId => ScopeOverride.Value ?? _tenantId;
    public bool HasTenant => TenantId != Guid.Empty;

    public void Set(Guid tenantId) => _tenantId = tenantId;

    public IDisposable BeginScope(Guid tenantId)
    {
        var previous = ScopeOverride.Value;
        ScopeOverride.Value = tenantId;
        return new Reset(previous);
    }

    private sealed class Reset(Guid? previous) : IDisposable
    {
        public void Dispose() => ScopeOverride.Value = previous;
    }
}

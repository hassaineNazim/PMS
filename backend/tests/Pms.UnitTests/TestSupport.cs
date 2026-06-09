using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Infrastructure.Persistence;

namespace Pms.UnitTests;

/// <summary>Fixed-tenant context used by unit tests.</summary>
public sealed class TestTenant(Guid id) : ICurrentTenant
{
    public Guid TenantId { get; } = id;
    public bool HasTenant => TenantId != Guid.Empty;
    public IDisposable BeginScope(Guid tenantId) => new Noop();
    private sealed class Noop : IDisposable { public void Dispose() { } }
}

public static class InMemoryDb
{
    /// <summary>Creates an isolated in-memory AppDbContext bound to a tenant.</summary>
    public static AppDbContext Create(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pms-tests-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;
        return new AppDbContext(options, new TestTenant(tenantId));
    }
}

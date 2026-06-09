using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pms.Application.Common;

namespace Pms.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef migrations` can construct the context without
/// the full DI graph. Uses a no-op tenant (migrations don't run query filters).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=pms;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, new DesignTimeTenant());
    }

    private sealed class DesignTimeTenant : ICurrentTenant
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
        public IDisposable BeginScope(Guid tenantId) => new NoopScope();
        private sealed class NoopScope : IDisposable { public void Dispose() { } }
    }
}

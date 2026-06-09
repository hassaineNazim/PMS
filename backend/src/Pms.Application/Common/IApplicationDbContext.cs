using Microsoft.EntityFrameworkCore;
using Pms.Domain.Entities;

namespace Pms.Application.Common;

/// <summary>
/// Abstraction over the EF Core context so the Application layer can query/persist
/// without depending on the Infrastructure project. The concrete AppDbContext in
/// Infrastructure implements this and applies tenant filtering + concurrency.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<License> Licenses { get; }
    DbSet<User> Users { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Guest> Guests { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<Staff> Staff { get; }
    DbSet<StaffSchedule> StaffSchedules { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Payment> Payments { get; }
    DbSet<CashSession> CashSessions { get; }
    DbSet<Charge> Charges { get; }
    DbSet<RatePeriod> RatePeriods { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a database transaction that is
    /// committed on success. The whole unit is executed through the provider's
    /// execution strategy, so it is compatible with retry-on-transient-failure
    /// (used to make check-in atomic and resilient).
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pms.Application.Common;
using Pms.Domain.Common;
using Pms.Domain.Entities;
using Pms.Domain.Exceptions;

namespace Pms.Infrastructure.Persistence;

/// <summary>
/// EF Core context implementing <see cref="IApplicationDbContext"/>. Responsibilities:
///  - applies a global query filter so every tenant-scoped query is automatically
///    constrained to the current tenant (data isolation),
///  - stamps UpdatedAt on save,
///  - enables PostgreSQL xmin optimistic concurrency for booking-critical tables,
///  - translates PostgreSQL unique/exclusion violations into domain ConflictExceptions.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffSchedule> StaffSchedules => Set<StaffSchedule>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<RatePeriod> RatePeriods => Set<RatePeriod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global multi-tenant query filter for every ITenantEntity. Each filter
        // references the context property CurrentTenantId; EF re-evaluates that
        // against the live context instance per query, so one cached model serves
        // every tenant safely.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var filter = (LambdaExpression)BuildTenantFilterMethod
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, null)!;
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }

            // Optimistic concurrency via PostgreSQL's xmin system column. Mapped as a
            // shadow property so two receptionists editing the same row can't silently
            // overwrite each other (a DbUpdateConcurrencyException is raised instead).
            if (Database.IsNpgsql() && typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    private static readonly System.Reflection.MethodInfo BuildTenantFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(BuildTenantFilter),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

    private LambdaExpression BuildTenantFilter<TEntity>() where TEntity : class, ITenantEntity
    {
        // Closes over `this`; EF rewrites the context reference per query instance.
        Expression<Func<TEntity, bool>> filter = e => e.TenantId == CurrentTenantId;
        return filter;
    }

    /// <summary>Exposed so the global query filter can reference the live tenant id.</summary>
    public Guid CurrentTenantId => currentTenant.TenantId;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            // 23P01 = exclusion_violation (overlapping booking), 23505 = unique_violation.
            if (pg.SqlState is "23P01")
                throw new ConflictException("The room is already booked for the selected dates.");
            if (pg.SqlState is "23505")
                throw new ConflictException("A record with the same unique value already exists.");
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await Database.BeginTransactionAsync(cancellationToken);
            await operation(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });
    }

    private void StampTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}

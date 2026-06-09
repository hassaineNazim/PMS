using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pms.Application.Common;
using Pms.Domain.Entities;
using Pms.Domain.Enums;

namespace Pms.Infrastructure.Persistence;

/// <summary>
/// Applies migrations, installs the database-level no-double-booking guarantee,
/// and seeds a demo tenant on first run. Safe to call on every startup.
/// </summary>
public class DbInitializer(
    AppDbContext db,
    IPasswordHasher hasher,
    ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (db.Database.IsNpgsql())
        {
            logger.LogInformation("Applying database migrations…");
            await db.Database.MigrateAsync(ct);
            await EnsureNoDoubleBookingConstraintAsync(ct);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }

        await SeedDemoTenantAsync(ct);
    }

    /// <summary>
    /// Installs a PostgreSQL EXCLUDE constraint that makes overlapping bookings for
    /// the same room physically impossible — the ultimate guard against a race
    /// between two receptionists confirming the same room at the same instant.
    /// Idempotent.
    /// </summary>
    private async Task EnsureNoDoubleBookingConstraintAsync(CancellationToken ct)
    {
        const string sql = """
            CREATE EXTENSION IF NOT EXISTS btree_gist;
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'no_overlapping_bookings'
                ) THEN
                    ALTER TABLE reservations
                        ADD CONSTRAINT no_overlapping_bookings
                        EXCLUDE USING gist (
                            room_id WITH =,
                            daterange(check_in, check_out, '[)') WITH &&
                        ) WHERE (status IN ('Confirmed', 'CheckedIn'));
                END IF;
            END $$;
            """;
        await db.Database.ExecuteSqlRawAsync(sql, ct);
        logger.LogInformation("No-double-booking EXCLUDE constraint ensured.");
    }

    private async Task SeedDemoTenantAsync(CancellationToken ct)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(ct))
            return;

        logger.LogInformation("Seeding demo tenant…");

        var tenant = new Tenant
        {
            Name = "Hôtel Démo",
            LegalName = "Hôtel Démo SARL",
            Slug = "demo",
            City = "Alger",
            Country = "Algérie",
            Currency = "DZD",
            DefaultTaxRate = 9.00m,
            ContactEmail = "contact@hoteldemo.dz",
            Phone = "+213 21 00 00 00",
            Address = "12 Rue Didouche Mourad",
            IsActive = true,
            // Algerian legal identifiers (demo values)
            TaxId = "000216001234567",
            StatId = "0982160012345",
            TradeRegister = "16/00-1234567B23",
            TaxArticle = "16550123456",
            FiscalStampEnabled = true,
            FiscalStampRate = 1.00m,
            FiscalStampMinimum = 5.00m,
            // Meal plan supplements (per person, per night)
            BreakfastSupplement = 800m,
            HalfBoardSupplement = 2500m,
            FullBoardSupplement = 4000m
        };
        tenant.License = new License
        {
            TenantId = tenant.Id,
            Key = "DEMO-0000-0000-0000",
            Plan = LicensePlan.Professional,
            MaxRooms = 0,
            MaxUsers = 0,
            ValidFrom = DateTimeOffset.UtcNow.AddDays(-1),
            ValidUntil = DateTimeOffset.UtcNow.AddYears(5),
            IsActive = true
        };
        db.Tenants.Add(tenant);

        db.Users.Add(new User
        {
            TenantId = tenant.Id,
            Email = "admin@demo.com",
            PasswordHash = hasher.Hash("admin123"),
            FullName = "Administrateur Démo",
            Role = UserRole.Admin,
            IsActive = true
        });

        var rooms = new[]
        {
            new Room { TenantId = tenant.Id, Number = "101", Type = RoomType.Single, Floor = 1, Capacity = 1, PricePerNight = 8900m },
            new Room { TenantId = tenant.Id, Number = "102", Type = RoomType.Double, Floor = 1, Capacity = 2, PricePerNight = 12900m },
            new Room { TenantId = tenant.Id, Number = "201", Type = RoomType.Suite, Floor = 2, Capacity = 4, PricePerNight = 29900m },
            new Room { TenantId = tenant.Id, Number = "202", Type = RoomType.Deluxe, Floor = 2, Capacity = 3, PricePerNight = 19900m }
        };
        db.Rooms.AddRange(rooms);

        db.Guests.AddRange(
            new Guest { TenantId = tenant.Id, FirstName = "Jean", LastName = "Dupont", Email = "jean.dupont@email.com", Language = "fr" },
            new Guest { TenantId = tenant.Id, FirstName = "John", LastName = "Smith", Email = "john.smith@email.com", Language = "en" });

        db.Staff.AddRange(
            new Staff { TenantId = tenant.Id, FirstName = "Marie", LastName = "Laurent", Email = "marie.laurent@hotel.dz", Role = StaffRole.Manager, Department = "Direction", HireDate = new DateOnly(2020, 3, 15), Status = StaffStatus.Active },
            new Staff { TenantId = tenant.Id, FirstName = "Pierre", LastName = "Martin", Email = "pierre.martin@hotel.dz", Role = StaffRole.Receptionist, Department = "Réception", HireDate = new DateOnly(2021, 6, 1), Status = StaffStatus.Active },
            new Staff { TenantId = tenant.Id, FirstName = "Sophie", LastName = "Bernard", Email = "sophie.bernard@hotel.dz", Role = StaffRole.Housekeeper, Department = "Étages", HireDate = new DateOnly(2022, 1, 10), Status = StaffStatus.Active });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo tenant seeded (login: admin@demo.com / admin123, slug: demo).");
    }
}

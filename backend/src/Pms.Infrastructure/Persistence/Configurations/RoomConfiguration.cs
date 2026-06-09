using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("rooms");
        b.HasKey(r => r.Id);
        b.Property(r => r.Number).IsRequired().HasMaxLength(10);
        b.Property(r => r.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(r => r.HousekeepingStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(r => r.PricePerNight).HasPrecision(10, 2);
        b.HasIndex(r => new { r.TenantId, r.Number }).IsUnique();
    }
}

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> b)
    {
        b.ToTable("guests");
        b.HasKey(g => g.Id);
        b.Property(g => g.FirstName).IsRequired().HasMaxLength(100);
        b.Property(g => g.LastName).IsRequired().HasMaxLength(100);
        b.Property(g => g.Email).HasMaxLength(255);
        b.Property(g => g.Language).HasMaxLength(5);
        b.Ignore(g => g.FullName);
        b.HasIndex(g => new { g.TenantId, g.Email });
        b.HasIndex(g => new { g.TenantId, g.LastName });
    }
}

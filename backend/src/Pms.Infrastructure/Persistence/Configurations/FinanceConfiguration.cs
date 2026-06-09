using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(p => p.Id);
        b.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.Amount).HasPrecision(12, 2);
        b.Property(p => p.StampDuty).HasPrecision(10, 2);
        b.Property(p => p.Reference).HasMaxLength(100);
        b.Ignore(p => p.SignedAmount);
        b.HasOne(p => p.Reservation).WithMany().HasForeignKey(p => p.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => new { p.TenantId, p.PaidAt });
        b.HasIndex(p => p.ReservationId);
        b.HasIndex(p => p.CashSessionId);
    }
}

public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> b)
    {
        b.ToTable("cash_sessions");
        b.HasKey(c => c.Id);
        b.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.UserName).HasMaxLength(200);
        b.Property(c => c.OpeningFloat).HasPrecision(12, 2);
        b.Property(c => c.CashMovements).HasPrecision(12, 2);
        b.Property(c => c.ExpectedCash).HasPrecision(12, 2);
        b.Property(c => c.CountedCash).HasPrecision(12, 2);
        b.Property(c => c.Discrepancy).HasPrecision(12, 2);
        b.HasIndex(c => new { c.TenantId, c.Status });
    }
}

public class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> b)
    {
        b.ToTable("charges");
        b.HasKey(c => c.Id);
        b.Property(c => c.Category).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.Label).IsRequired().HasMaxLength(200);
        b.Property(c => c.UnitPrice).HasPrecision(10, 2);
        b.Property(c => c.Total).HasPrecision(12, 2);
        b.HasOne(c => c.Reservation).WithMany().HasForeignKey(c => c.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(c => c.ReservationId);
    }
}

public class RatePeriodConfiguration : IEntityTypeConfiguration<RatePeriod>
{
    public void Configure(EntityTypeBuilder<RatePeriod> b)
    {
        b.ToTable("rate_periods");
        b.HasKey(r => r.Id);
        b.Property(r => r.Name).IsRequired().HasMaxLength(120);
        b.Property(r => r.RoomType).HasConversion<string>().HasMaxLength(20);
        b.Property(r => r.PricePerNight).HasPrecision(10, 2);
        b.HasIndex(r => new { r.TenantId, r.StartDate, r.EndDate });
    }
}

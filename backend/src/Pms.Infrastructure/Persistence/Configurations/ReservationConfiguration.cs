using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b)
    {
        b.ToTable("reservations");
        b.HasKey(r => r.Id);
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(r => r.MealPlan).HasConversion<string>().HasMaxLength(20);
        b.Property(r => r.MealPlanSupplement).HasPrecision(10, 2);
        b.Property(r => r.TotalAmount).HasPrecision(10, 2);
        b.Ignore(r => r.Nights);
        b.Ignore(r => r.Occupants);
        b.Ignore(r => r.MealPlanTotal);

        b.HasOne(r => r.Guest).WithMany().HasForeignKey(r => r.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Room).WithMany().HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(r => new { r.RoomId, r.CheckIn, r.CheckOut });
        b.HasIndex(r => r.Status);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("invoices");
        b.HasKey(i => i.Id);
        b.Property(i => i.Number).IsRequired().HasMaxLength(40);
        b.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(i => i.Currency).HasMaxLength(3);
        b.Property(i => i.PricePerNight).HasPrecision(10, 2);
        b.Property(i => i.RoomSubtotal).HasPrecision(12, 2);
        b.Property(i => i.MealPlanSubtotal).HasPrecision(12, 2);
        b.Property(i => i.ExtrasSubtotal).HasPrecision(12, 2);
        b.Property(i => i.Subtotal).HasPrecision(12, 2);
        b.Property(i => i.TaxRate).HasPrecision(5, 2);
        b.Property(i => i.TaxAmount).HasPrecision(12, 2);
        b.Property(i => i.StampDuty).HasPrecision(10, 2);
        b.Property(i => i.Total).HasPrecision(12, 2);
        b.Property(i => i.AmountPaid).HasPrecision(12, 2);
        b.Property(i => i.BalanceDue).HasPrecision(12, 2);

        b.HasOne(i => i.Guest).WithMany().HasForeignKey(i => i.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(i => i.Room).WithMany().HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(i => i.Reservation).WithMany().HasForeignKey(i => i.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(i => new { i.TenantId, i.Number }).IsUnique();
    }
}

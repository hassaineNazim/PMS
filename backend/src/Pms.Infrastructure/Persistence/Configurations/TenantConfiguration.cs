using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.HasKey(t => t.Id);
        b.Property(t => t.Name).IsRequired().HasMaxLength(200);
        b.Property(t => t.LegalName).HasMaxLength(200);
        b.Property(t => t.Slug).IsRequired().HasMaxLength(60);
        b.HasIndex(t => t.Slug).IsUnique();
        b.Property(t => t.Currency).IsRequired().HasMaxLength(3);
        b.Property(t => t.DefaultTaxRate).HasPrecision(5, 2);
        b.Property(t => t.TaxId).HasMaxLength(30);
        b.Property(t => t.StatId).HasMaxLength(30);
        b.Property(t => t.TradeRegister).HasMaxLength(40);
        b.Property(t => t.TaxArticle).HasMaxLength(40);
        b.Property(t => t.FiscalStampRate).HasPrecision(5, 2);
        b.Property(t => t.FiscalStampMinimum).HasPrecision(10, 2);
        b.Property(t => t.BreakfastSupplement).HasPrecision(10, 2);
        b.Property(t => t.HalfBoardSupplement).HasPrecision(10, 2);
        b.Property(t => t.FullBoardSupplement).HasPrecision(10, 2);
        b.HasOne(t => t.License).WithOne(l => l.Tenant!)
            .HasForeignKey<License>(l => l.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> b)
    {
        b.ToTable("licenses");
        b.HasKey(l => l.Id);
        b.Property(l => l.Key).IsRequired().HasMaxLength(80);
        b.HasIndex(l => l.Key).IsUnique();
        b.Property(l => l.Plan).HasConversion<string>().HasMaxLength(20);
    }
}

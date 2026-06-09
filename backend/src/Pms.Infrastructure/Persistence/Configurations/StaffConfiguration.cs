using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StaffEntity = Pms.Domain.Entities.Staff;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<StaffEntity>
{
    public void Configure(EntityTypeBuilder<StaffEntity> b)
    {
        b.ToTable("staff");
        b.HasKey(s => s.Id);
        b.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
        b.Property(s => s.LastName).IsRequired().HasMaxLength(100);
        b.Property(s => s.Email).HasMaxLength(255);
        b.Property(s => s.Role).HasConversion<string>().HasMaxLength(20);
        b.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(s => s.Department).HasMaxLength(100);
        b.Ignore(s => s.FullName);
        b.HasMany(s => s.Schedules).WithOne(sc => sc.Staff!)
            .HasForeignKey(sc => sc.StaffId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(s => new { s.TenantId, s.Status });
    }
}

public class StaffScheduleConfiguration : IEntityTypeConfiguration<StaffSchedule>
{
    public void Configure(EntityTypeBuilder<StaffSchedule> b)
    {
        b.ToTable("staff_schedules");
        b.HasKey(s => s.Id);
        b.Property(s => s.Notes).HasMaxLength(500);
        b.HasIndex(s => new { s.TenantId, s.Date });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(a => a.Id);
        b.Property(a => a.Action).IsRequired().HasMaxLength(100);
        b.Property(a => a.EntityType).HasMaxLength(50);
        b.Property(a => a.Details).HasColumnType("jsonb");
        b.HasIndex(a => new { a.TenantId, a.CreatedAt });
    }
}

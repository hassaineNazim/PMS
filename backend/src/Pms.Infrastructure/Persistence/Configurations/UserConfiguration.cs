using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).IsRequired().HasMaxLength(255);
        b.Property(u => u.PasswordHash).IsRequired();
        b.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        b.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
    }
}

using Everlore.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.HasKey(tu => tu.Id);

        builder.Property(tu => tu.Role)
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne(tu => tu.User)
            .WithMany(u => u.TenantUsers)
            .HasForeignKey(tu => tu.UserId);

        builder.HasOne(tu => tu.Tenant)
            .WithMany(t => t.TenantUsers)
            .HasForeignKey(tu => tu.TenantId);

        builder.HasIndex(tu => new { tu.UserId, tu.TenantId }).IsUnique();
    }
}

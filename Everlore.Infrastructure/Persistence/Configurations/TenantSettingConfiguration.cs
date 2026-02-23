using Everlore.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> builder)
    {
        builder.HasKey(ts => ts.Id);
        builder.Property(ts => ts.Key).HasMaxLength(200).IsRequired();
        builder.Property(ts => ts.Value).HasMaxLength(2000).IsRequired();
        builder.Property(ts => ts.Description).HasMaxLength(500);

        builder.HasIndex(ts => new { ts.TenantId, ts.Key }).IsUnique();

        builder.HasOne(ts => ts.Tenant)
            .WithMany()
            .HasForeignKey(ts => ts.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

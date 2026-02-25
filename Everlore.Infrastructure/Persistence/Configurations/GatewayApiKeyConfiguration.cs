using Everlore.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class GatewayApiKeyConfiguration : IEntityTypeConfiguration<GatewayApiKey>
{
    public void Configure(EntityTypeBuilder<GatewayApiKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Name).HasMaxLength(200).IsRequired();
        builder.Property(k => k.KeyHash).HasMaxLength(64).IsRequired();
        builder.Property(k => k.KeyPrefix).HasMaxLength(16).IsRequired();

        builder.HasIndex(k => k.KeyHash).IsUnique();
        builder.HasIndex(k => k.TenantId);

        builder.HasOne(k => k.Tenant)
            .WithMany()
            .HasForeignKey(k => k.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

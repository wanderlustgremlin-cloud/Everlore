using Everlore.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class ConnectorConfigurationConfiguration : IEntityTypeConfiguration<ConnectorConfiguration>
{
    public void Configure(EntityTypeBuilder<ConnectorConfiguration> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ConnectorType).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Settings);

        builder.HasOne(c => c.Tenant)
            .WithMany(t => t.ConnectorConfigurations)
            .HasForeignKey(c => c.TenantId);

        builder.HasIndex(c => new { c.TenantId, c.ConnectorType }).IsUnique();
    }
}

using Everlore.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>

{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Identifier).HasMaxLength(100).IsRequired();
        builder.Property(t => t.ConnectionString).HasMaxLength(500);
        builder.Property(t => t.HostingMode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(HostingMode.SaasHosted)
            .IsRequired();

        builder.HasIndex(t => t.Identifier).IsUnique();
    }
}

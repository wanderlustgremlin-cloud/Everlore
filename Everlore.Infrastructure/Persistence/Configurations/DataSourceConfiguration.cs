using Everlore.Domain.Reporting;
using Everlore.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> builder)
    {
        builder.HasKey(ds => ds.Id);
        builder.Property(ds => ds.Name).HasMaxLength(200).IsRequired();
        builder.Property(ds => ds.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(ds => ds.EncryptedConnectionString).HasMaxLength(2000).IsRequired();
        builder.HasIndex(ds => new { ds.TenantId, ds.Name }).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(ds => ds.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

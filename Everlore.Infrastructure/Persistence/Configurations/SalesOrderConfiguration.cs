using Everlore.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.HasKey(so => so.Id);
        builder.Property(so => so.OrderNumber).HasMaxLength(100).IsRequired();
        builder.Property(so => so.Status).HasMaxLength(50).IsRequired();
        builder.Property(so => so.TotalAmount).HasPrecision(18, 2);

        builder.HasOne(so => so.Customer)
            .WithMany()
            .HasForeignKey(so => so.CustomerId);

        builder.HasIndex(so => so.OrderNumber).IsUnique();
    }
}

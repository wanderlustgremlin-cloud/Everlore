using Everlore.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.HasKey(sol => sol.Id);
        builder.Property(sol => sol.UnitPrice).HasPrecision(18, 2);
        builder.Property(sol => sol.LineTotal).HasPrecision(18, 2);

        builder.HasOne(sol => sol.SalesOrder)
            .WithMany(so => so.Lines)
            .HasForeignKey(sol => sol.SalesOrderId);

        builder.HasOne(sol => sol.Product)
            .WithMany()
            .HasForeignKey(sol => sol.ProductId);
    }
}

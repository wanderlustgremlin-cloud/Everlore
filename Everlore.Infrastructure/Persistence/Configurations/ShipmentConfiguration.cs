using Everlore.Domain.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TrackingNumber).HasMaxLength(200);
        builder.Property(s => s.Status).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ShipToAddress).HasMaxLength(500);

        builder.HasOne(s => s.Carrier)
            .WithMany(c => c.Shipments)
            .HasForeignKey(s => s.CarrierId);

        builder.HasOne(s => s.SalesOrder)
            .WithMany()
            .HasForeignKey(s => s.SalesOrderId)
            .IsRequired(false);
    }
}

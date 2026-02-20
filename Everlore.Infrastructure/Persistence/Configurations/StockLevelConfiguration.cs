using Everlore.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.HasKey(sl => sl.Id);
        builder.Ignore(sl => sl.QuantityAvailable);

        builder.HasOne(sl => sl.Product)
            .WithMany(p => p.StockLevels)
            .HasForeignKey(sl => sl.ProductId);

        builder.HasOne(sl => sl.Warehouse)
            .WithMany(w => w.StockLevels)
            .HasForeignKey(sl => sl.WarehouseId);

        builder.HasIndex(sl => new { sl.ProductId, sl.WarehouseId }).IsUnique();
    }
}

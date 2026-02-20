using Everlore.Domain.AccountsPayable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BillNumber).HasMaxLength(100).IsRequired();
        builder.Property(b => b.TotalAmount).HasPrecision(18, 2);
        builder.Property(b => b.AmountPaid).HasPrecision(18, 2);
        builder.Property(b => b.Status).HasMaxLength(50).IsRequired();

        builder.HasOne(b => b.Vendor)
            .WithMany(v => v.Bills)
            .HasForeignKey(b => b.VendorId);

        builder.HasIndex(b => b.BillNumber).IsUnique();
    }
}

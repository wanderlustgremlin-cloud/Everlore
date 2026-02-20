using Everlore.Domain.AccountsPayable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class BillPaymentConfiguration : IEntityTypeConfiguration<BillPayment>
{
    public void Configure(EntityTypeBuilder<BillPayment> builder)
    {
        builder.HasKey(bp => bp.Id);
        builder.Property(bp => bp.Amount).HasPrecision(18, 2);
        builder.Property(bp => bp.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(bp => bp.ReferenceNumber).HasMaxLength(100);

        builder.HasOne(bp => bp.Bill)
            .WithMany(b => b.Payments)
            .HasForeignKey(bp => bp.BillId);
    }
}

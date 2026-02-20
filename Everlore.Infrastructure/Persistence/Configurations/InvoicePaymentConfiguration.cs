using Everlore.Domain.AccountsReceivable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Everlore.Infrastructure.Persistence.Configurations;

public class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.HasKey(ip => ip.Id);
        builder.Property(ip => ip.Amount).HasPrecision(18, 2);
        builder.Property(ip => ip.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(ip => ip.ReferenceNumber).HasMaxLength(100);

        builder.HasOne(ip => ip.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(ip => ip.InvoiceId);
    }
}

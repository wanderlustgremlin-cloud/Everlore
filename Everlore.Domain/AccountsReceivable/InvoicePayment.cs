using Everlore.Domain.Common;

namespace Everlore.Domain.AccountsReceivable;

public class InvoicePayment : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }

    public Invoice Invoice { get; set; } = null!;
}

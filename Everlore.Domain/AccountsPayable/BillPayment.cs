using Everlore.Domain.Common;

namespace Everlore.Domain.AccountsPayable;

public class BillPayment : BaseEntity
{
    public Guid BillId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }

    public Bill Bill { get; set; } = null!;
}

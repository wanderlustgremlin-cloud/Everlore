using Everlore.Domain.Common;

namespace Everlore.Domain.AccountsPayable;

public class Bill : BaseEntity
{
    public Guid VendorId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;

    public Vendor Vendor { get; set; } = null!;
    public ICollection<BillPayment> Payments { get; set; } = [];
}

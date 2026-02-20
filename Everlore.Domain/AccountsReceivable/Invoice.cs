using Everlore.Domain.Common;

namespace Everlore.Domain.AccountsReceivable;

public class Invoice : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? SalesOrderId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Sales.SalesOrder? SalesOrder { get; set; }
    public ICollection<InvoicePayment> Payments { get; set; } = [];
}

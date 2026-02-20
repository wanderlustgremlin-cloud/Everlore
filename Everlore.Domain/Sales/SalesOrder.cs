using Everlore.Domain.Common;

namespace Everlore.Domain.Sales;

public class SalesOrder : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    public AccountsReceivable.Customer Customer { get; set; } = null!;
    public ICollection<SalesOrderLine> Lines { get; set; } = [];
}

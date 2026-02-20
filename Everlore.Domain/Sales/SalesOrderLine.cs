using Everlore.Domain.Common;

namespace Everlore.Domain.Sales;

public class SalesOrderLine : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public SalesOrder SalesOrder { get; set; } = null!;
    public Inventory.Product Product { get; set; } = null!;
}

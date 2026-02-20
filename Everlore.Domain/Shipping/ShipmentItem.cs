using Everlore.Domain.Common;

namespace Everlore.Domain.Shipping;

public class ShipmentItem : BaseEntity
{
    public Guid ShipmentId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }

    public Shipment Shipment { get; set; } = null!;
    public Inventory.Product Product { get; set; } = null!;
}

using Everlore.Domain.Common;

namespace Everlore.Domain.Shipping;

public class Shipment : BaseEntity
{
    public Guid CarrierId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string? TrackingNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? ShipToAddress { get; set; }

    public Carrier Carrier { get; set; } = null!;
    public Sales.SalesOrder? SalesOrder { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = [];
}

using Bogus;
using Everlore.Domain.Sales;
using Everlore.Domain.Shipping;

namespace Everlore.Connector.Seed.Generators;

internal static class ShipmentGenerator
{
    public static (List<Shipment> Shipments, List<ShipmentItem> Items) Generate(
        List<SalesOrder> orders, List<SalesOrderLine> orderLines, List<Carrier> carriers, Faker faker)
    {
        var shipments = new List<Shipment>();
        var items = new List<ShipmentItem>();

        var eligibleOrders = orders.Where(o => o.Status is "Shipped" or "Delivered").ToList();
        var linesByOrder = orderLines.ToLookup(l => l.SalesOrderId);

        foreach (var order in eligibleOrders)
        {
            var shippedDate = order.OrderDate.AddDays(faker.Random.Int(1, 5));
            var isDelivered = order.Status == "Delivered";

            var shipment = new Shipment
            {
                Id = faker.Random.Guid(),
                CarrierId = faker.PickRandom(carriers).Id,
                SalesOrderId = order.Id,
                TrackingNumber = faker.Random.AlphaNumeric(12).ToUpper(),
                Status = isDelivered ? "Delivered" : "InTransit",
                ShippedDate = shippedDate,
                DeliveredDate = isDelivered ? shippedDate.AddDays(faker.Random.Int(1, 7)) : null,
                ShipToAddress = faker.Address.FullAddress()
            };

            shipments.Add(shipment);

            foreach (var line in linesByOrder[order.Id])
            {
                items.Add(new ShipmentItem
                {
                    Id = faker.Random.Guid(),
                    ShipmentId = shipment.Id,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity
                });
            }
        }

        return (shipments, items);
    }
}

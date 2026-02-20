using Bogus;
using Everlore.Domain.AccountsReceivable;
using Everlore.Domain.Inventory;
using Everlore.Domain.Sales;

namespace Everlore.Connector.Seed.Generators;

internal static class SalesOrderGenerator
{
    private static readonly string[] Statuses = ["Pending", "Confirmed", "Shipped", "Delivered", "Cancelled"];

    public static (List<SalesOrder> Orders, List<SalesOrderLine> Lines) Generate(
        List<Customer> customers, List<Product> products, int ordersPerCustomer, int maxLines, Faker faker)
    {
        var orders = new List<SalesOrder>();
        var lines = new List<SalesOrderLine>();
        var orderNumber = 1;

        foreach (var customer in customers)
        {
            for (var i = 0; i < ordersPerCustomer; i++)
            {
                var status = faker.PickRandom(Statuses);
                var orderId = faker.Random.Guid();
                var orderDate = faker.Date.Past(1).ToUniversalTime();
                var lineCount = faker.Random.Int(1, maxLines);
                var orderLines = new List<SalesOrderLine>();

                for (var j = 0; j < lineCount; j++)
                {
                    var product = faker.PickRandom(products);
                    var qty = faker.Random.Int(1, 20);
                    var lineTotal = Math.Round(product.UnitPrice * qty, 2);

                    orderLines.Add(new SalesOrderLine
                    {
                        Id = faker.Random.Guid(),
                        SalesOrderId = orderId,
                        ProductId = product.Id,
                        Quantity = qty,
                        UnitPrice = product.UnitPrice,
                        LineTotal = lineTotal
                    });
                }

                var totalAmount = orderLines.Sum(l => l.LineTotal);

                orders.Add(new SalesOrder
                {
                    Id = orderId,
                    CustomerId = customer.Id,
                    OrderNumber = $"SO-{orderNumber++:D5}",
                    OrderDate = orderDate,
                    Status = status,
                    TotalAmount = totalAmount
                });

                lines.AddRange(orderLines);
            }
        }

        return (orders, lines);
    }
}

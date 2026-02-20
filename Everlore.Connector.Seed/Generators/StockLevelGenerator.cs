using Bogus;
using Everlore.Domain.Inventory;

namespace Everlore.Connector.Seed.Generators;

internal static class StockLevelGenerator
{
    public static List<StockLevel> Generate(List<Product> products, List<Warehouse> warehouses, Faker faker)
    {
        var stockLevels = new List<StockLevel>();

        foreach (var product in products)
        {
            foreach (var warehouse in warehouses)
            {
                var onHand = faker.Random.Int(0, 500);
                var reserved = faker.Random.Int(0, Math.Min(onHand, 100));

                stockLevels.Add(new StockLevel
                {
                    Id = faker.Random.Guid(),
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    QuantityOnHand = onHand,
                    QuantityReserved = reserved
                });
            }
        }

        return stockLevels;
    }
}

using Bogus;
using Everlore.Domain.Inventory;

namespace Everlore.Connector.Seed.Generators;

internal static class ProductGenerator
{
    private static readonly string[] Units = ["EA", "KG", "LB", "PK", "CS", "BX"];

    public static List<Product> Generate(int count, Faker faker)
    {
        return Enumerable.Range(0, count).Select(i => new Product
        {
            Id = faker.Random.Guid(),
            Sku = $"SKU-{i + 1:D5}",
            Name = faker.Commerce.ProductName(),
            Description = faker.Commerce.ProductDescription(),
            UnitPrice = Math.Round(faker.Random.Decimal(5m, 500m), 2),
            UnitOfMeasure = faker.PickRandom(Units),
            IsActive = true
        }).ToList();
    }
}

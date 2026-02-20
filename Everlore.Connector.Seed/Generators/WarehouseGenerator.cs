using Bogus;
using Everlore.Domain.Inventory;

namespace Everlore.Connector.Seed.Generators;

internal static class WarehouseGenerator
{
    public static List<Warehouse> Generate(int count, Faker faker)
    {
        return Enumerable.Range(0, count).Select(i => new Warehouse
        {
            Id = faker.Random.Guid(),
            Name = $"{faker.Address.City()} Warehouse",
            Code = $"WH-{i + 1:D3}",
            Address = faker.Address.FullAddress(),
            IsActive = true
        }).ToList();
    }
}

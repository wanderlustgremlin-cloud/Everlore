using Bogus;
using Everlore.Domain.Shipping;

namespace Everlore.Connector.Seed.Generators;

internal static class CarrierGenerator
{
    public static List<Carrier> Generate(int count, Faker faker)
    {
        return Enumerable.Range(0, count).Select(i => new Carrier
        {
            Id = faker.Random.Guid(),
            Name = $"{faker.Company.CompanyName()} Logistics",
            Code = $"CR-{i + 1:D3}",
            ContactEmail = faker.Internet.Email(),
            Phone = faker.Phone.PhoneNumber(),
            IsActive = true
        }).ToList();
    }
}

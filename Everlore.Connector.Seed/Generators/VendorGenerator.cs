using Bogus;
using Everlore.Domain.AccountsPayable;

namespace Everlore.Connector.Seed.Generators;

internal static class VendorGenerator
{
    public static List<Vendor> Generate(int count, Faker faker)
    {
        return Enumerable.Range(0, count).Select(_ => new Vendor
        {
            Id = faker.Random.Guid(),
            Name = faker.Company.CompanyName(),
            ContactEmail = faker.Internet.Email(),
            Phone = faker.Phone.PhoneNumber(),
            Address = faker.Address.FullAddress(),
            IsActive = true
        }).ToList();
    }
}

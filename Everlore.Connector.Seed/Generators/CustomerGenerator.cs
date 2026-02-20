using Bogus;
using Everlore.Domain.AccountsReceivable;

namespace Everlore.Connector.Seed.Generators;

internal static class CustomerGenerator
{
    public static List<Customer> Generate(int count, Faker faker)
    {
        return Enumerable.Range(0, count).Select(_ => new Customer
        {
            Id = faker.Random.Guid(),
            Name = faker.Company.CompanyName(),
            ContactEmail = faker.Internet.Email(),
            Phone = faker.Phone.PhoneNumber(),
            BillingAddress = faker.Address.FullAddress(),
            IsActive = true
        }).ToList();
    }
}

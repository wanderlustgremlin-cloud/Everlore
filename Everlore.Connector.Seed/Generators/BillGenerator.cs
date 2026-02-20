using Bogus;
using Everlore.Domain.AccountsPayable;

namespace Everlore.Connector.Seed.Generators;

internal static class BillGenerator
{
    private static readonly string[] Statuses = ["Draft", "Pending", "Paid", "Overdue"];

    public static (List<Bill> Bills, List<BillPayment> Payments) Generate(
        List<Vendor> vendors, int billsPerVendor, Faker faker)
    {
        var bills = new List<Bill>();
        var payments = new List<BillPayment>();
        var billNumber = 1;

        foreach (var vendor in vendors)
        {
            for (var i = 0; i < billsPerVendor; i++)
            {
                var status = faker.PickRandom(Statuses);
                var totalAmount = Math.Round(faker.Random.Decimal(100m, 10000m), 2);
                var billDate = faker.Date.Past(1).ToUniversalTime();

                var bill = new Bill
                {
                    Id = faker.Random.Guid(),
                    VendorId = vendor.Id,
                    BillNumber = $"BILL-{billNumber++:D5}",
                    BillDate = billDate,
                    DueDate = billDate.AddDays(30),
                    TotalAmount = totalAmount,
                    AmountPaid = status == "Paid" ? totalAmount : 0m,
                    Status = status
                };

                bills.Add(bill);

                if (status == "Paid")
                {
                    payments.Add(new BillPayment
                    {
                        Id = faker.Random.Guid(),
                        BillId = bill.Id,
                        Amount = totalAmount,
                        PaymentDate = billDate.AddDays(faker.Random.Int(1, 28)),
                        PaymentMethod = faker.PickRandom("ACH", "Wire", "Check", "Credit Card"),
                        ReferenceNumber = faker.Random.AlphaNumeric(10).ToUpper()
                    });
                }
            }
        }

        return (bills, payments);
    }
}

using Bogus;
using Everlore.Domain.AccountsReceivable;
using Everlore.Domain.Sales;

namespace Everlore.Connector.Seed.Generators;

internal static class InvoiceGenerator
{
    public static (List<Invoice> Invoices, List<InvoicePayment> Payments) Generate(
        List<SalesOrder> orders, Faker faker)
    {
        var invoices = new List<Invoice>();
        var payments = new List<InvoicePayment>();
        var invoiceNumber = 1;

        var eligibleOrders = orders.Where(o => o.Status is "Shipped" or "Delivered").ToList();

        foreach (var order in eligibleOrders)
        {
            var isPaid = order.Status == "Delivered";
            var invoiceDate = order.OrderDate.AddDays(faker.Random.Int(1, 5));

            var invoice = new Invoice
            {
                Id = faker.Random.Guid(),
                CustomerId = order.CustomerId,
                InvoiceNumber = $"INV-{invoiceNumber++:D5}",
                InvoiceDate = invoiceDate,
                DueDate = invoiceDate.AddDays(30),
                TotalAmount = order.TotalAmount,
                AmountPaid = isPaid ? order.TotalAmount : 0m,
                Status = isPaid ? "Paid" : "Sent",
                SalesOrderId = order.Id
            };

            invoices.Add(invoice);

            if (isPaid)
            {
                payments.Add(new InvoicePayment
                {
                    Id = faker.Random.Guid(),
                    InvoiceId = invoice.Id,
                    Amount = order.TotalAmount,
                    PaymentDate = invoiceDate.AddDays(faker.Random.Int(1, 28)),
                    PaymentMethod = faker.PickRandom("ACH", "Wire", "Check", "Credit Card"),
                    ReferenceNumber = faker.Random.AlphaNumeric(10).ToUpper()
                });
            }
        }

        return (invoices, payments);
    }
}

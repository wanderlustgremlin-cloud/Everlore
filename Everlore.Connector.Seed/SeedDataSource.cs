using System.Text.Json;
using Bogus;
using Everlore.Connector.Seed.Generators;
using Everlore.Domain.AccountsPayable;
using Everlore.Domain.AccountsReceivable;
using Everlore.Domain.Inventory;
using Everlore.Domain.Sales;
using Everlore.Domain.Shipping;
using Everlore.Infrastructure.Connectors;

namespace Everlore.Connector.Seed;

public class SeedDataSource : IConnectorSource
{
    public string ConnectorType => "seed";

    public IReadOnlyList<Vendor> Vendors { get; private set; } = [];
    public IReadOnlyList<Bill> Bills { get; private set; } = [];
    public IReadOnlyList<BillPayment> BillPayments { get; private set; } = [];
    public IReadOnlyList<Customer> Customers { get; private set; } = [];
    public IReadOnlyList<Invoice> Invoices { get; private set; } = [];
    public IReadOnlyList<InvoicePayment> InvoicePayments { get; private set; } = [];
    public IReadOnlyList<Product> Products { get; private set; } = [];
    public IReadOnlyList<Warehouse> Warehouses { get; private set; } = [];
    public IReadOnlyList<StockLevel> StockLevels { get; private set; } = [];
    public IReadOnlyList<Carrier> Carriers { get; private set; } = [];
    public IReadOnlyList<SalesOrder> SalesOrders { get; private set; } = [];
    public IReadOnlyList<SalesOrderLine> SalesOrderLines { get; private set; } = [];
    public IReadOnlyList<Shipment> Shipments { get; private set; } = [];
    public IReadOnlyList<ShipmentItem> ShipmentItems { get; private set; } = [];

    public Task InitializeAsync(string? settings, CancellationToken ct = default)
    {
        var cfg = settings is not null
            ? JsonSerializer.Deserialize<SeedSettings>(settings) ?? new SeedSettings()
            : new SeedSettings();

        var faker = new Faker { Random = new Randomizer(cfg.RandomSeed) };

        // 1. Independent roots
        var products = ProductGenerator.Generate(cfg.Products, faker);
        var warehouses = WarehouseGenerator.Generate(cfg.Warehouses, faker);
        var carriers = CarrierGenerator.Generate(cfg.Carriers, faker);
        var vendors = VendorGenerator.Generate(cfg.Vendors, faker);
        var customers = CustomerGenerator.Generate(cfg.Customers, faker);

        // 2. Product × Warehouse
        var stockLevels = StockLevelGenerator.Generate(products, warehouses, faker);

        // 3. Bills + payments (Vendor)
        var (bills, billPayments) = BillGenerator.Generate(vendors, cfg.BillsPerVendor, faker);

        // 4. Sales orders + lines (Customer + Product)
        var (salesOrders, salesOrderLines) = SalesOrderGenerator.Generate(
            customers, products, cfg.SalesOrdersPerCustomer, cfg.LinesPerSalesOrder, faker);

        // 5. Invoices + payments (linked to Shipped/Delivered orders)
        var (invoices, invoicePayments) = InvoiceGenerator.Generate(salesOrders, faker);

        // 6. Shipments + items (linked to Shipped/Delivered orders)
        var (shipments, shipmentItems) = ShipmentGenerator.Generate(salesOrders, salesOrderLines, carriers, faker);

        // Assign to properties
        Products = products;
        Warehouses = warehouses;
        Carriers = carriers;
        Vendors = vendors;
        Customers = customers;
        StockLevels = stockLevels;
        Bills = bills;
        BillPayments = billPayments;
        SalesOrders = salesOrders;
        SalesOrderLines = salesOrderLines;
        Invoices = invoices;
        InvoicePayments = invoicePayments;
        Shipments = shipments;
        ShipmentItems = shipmentItems;

        return Task.CompletedTask;
    }
}

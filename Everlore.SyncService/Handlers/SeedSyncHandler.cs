using Everlore.Connector.Seed;
using Everlore.Infrastructure.Connectors;
using Everlore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Everlore.SyncService.Handlers;

public class SeedSyncHandler(ILogger<SeedSyncHandler> logger) : ISyncHandler
{
    public string ConnectorType => "seed";

    public async Task SyncAsync(IConnectorSource source, EverloreDbContext targetDb, CancellationToken ct = default)
    {
        var seedSource = (SeedDataSource)source;

        if (await targetDb.Vendors.AnyAsync(ct))
        {
            logger.LogInformation("Tenant already has data, skipping seed sync");
            return;
        }

        targetDb.Vendors.AddRange(seedSource.Vendors);
        targetDb.Bills.AddRange(seedSource.Bills);
        targetDb.BillPayments.AddRange(seedSource.BillPayments);
        targetDb.Customers.AddRange(seedSource.Customers);
        targetDb.Invoices.AddRange(seedSource.Invoices);
        targetDb.InvoicePayments.AddRange(seedSource.InvoicePayments);
        targetDb.Products.AddRange(seedSource.Products);
        targetDb.Warehouses.AddRange(seedSource.Warehouses);
        targetDb.StockLevels.AddRange(seedSource.StockLevels);
        targetDb.Carriers.AddRange(seedSource.Carriers);
        targetDb.SalesOrders.AddRange(seedSource.SalesOrders);
        targetDb.SalesOrderLines.AddRange(seedSource.SalesOrderLines);
        targetDb.Shipments.AddRange(seedSource.Shipments);
        targetDb.ShipmentItems.AddRange(seedSource.ShipmentItems);

        await targetDb.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seeded {Vendors} vendors, {Customers} customers, {Products} products, {Orders} orders",
            seedSource.Vendors.Count, seedSource.Customers.Count,
            seedSource.Products.Count, seedSource.SalesOrders.Count);
    }
}

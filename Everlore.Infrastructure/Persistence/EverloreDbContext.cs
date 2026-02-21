using Everlore.Domain.AccountsPayable;
using Everlore.Domain.AccountsReceivable;
using Everlore.Domain.Common;
using Everlore.Domain.Inventory;
using Everlore.Domain.Sales;
using Everlore.Domain.Shipping;
using Everlore.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Infrastructure.Persistence;

public class EverloreDbContext : DbContext
{
    public EverloreDbContext(DbContextOptions<EverloreDbContext> options) : base(options) { }

    // Accounts Payable
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillPayment> BillPayments => Set<BillPayment>();

    // Accounts Receivable
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();

    // Inventory
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();

    // Shipping
    public DbSet<Carrier> Carriers => Set<Carrier>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();

    // Sales
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(EverloreDbContext).Assembly,
            type => type != typeof(Configurations.TenantConfiguration)
                 && type != typeof(Configurations.ConnectorConfigurationConfiguration)
                 && type != typeof(Configurations.TenantUserConfiguration));
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}

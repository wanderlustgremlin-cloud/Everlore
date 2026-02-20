namespace Everlore.Connector.Seed;

public class SeedSettings
{
    public int Vendors { get; set; } = 15;
    public int BillsPerVendor { get; set; } = 4;
    public int Customers { get; set; } = 25;
    public int Products { get; set; } = 40;
    public int Warehouses { get; set; } = 3;
    public int Carriers { get; set; } = 5;
    public int SalesOrdersPerCustomer { get; set; } = 3;
    public int LinesPerSalesOrder { get; set; } = 3;
    public int RandomSeed { get; set; } = 12345;
}

using Everlore.Domain.Common;

namespace Everlore.Domain.Inventory;

public class Product : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<StockLevel> StockLevels { get; set; } = [];
}

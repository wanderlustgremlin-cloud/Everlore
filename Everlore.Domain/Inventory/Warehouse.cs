using Everlore.Domain.Common;

namespace Everlore.Domain.Inventory;

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StockLevel> StockLevels { get; set; } = [];
}

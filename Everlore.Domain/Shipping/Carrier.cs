using Everlore.Domain.Common;

namespace Everlore.Domain.Shipping;

public class Carrier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Shipment> Shipments { get; set; } = [];
}

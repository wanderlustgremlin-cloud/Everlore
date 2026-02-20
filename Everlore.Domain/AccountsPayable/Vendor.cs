using Everlore.Domain.Common;

namespace Everlore.Domain.AccountsPayable;

public class Vendor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Bill> Bills { get; set; } = [];
}

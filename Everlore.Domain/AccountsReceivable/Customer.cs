using Everlore.Domain.Common;

namespace Everlore.Domain.AccountsReceivable;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public string? BillingAddress { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Invoice> Invoices { get; set; } = [];
}

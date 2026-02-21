using Microsoft.AspNetCore.Identity;

namespace Everlore.Domain.Tenancy;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<TenantUser> TenantUsers { get; set; } = [];
}

namespace Everlore.Domain.Tenancy;

public class TenantUser
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public TenantRole Role { get; set; } = TenantRole.Member;
    public DateTime CreatedAt { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}

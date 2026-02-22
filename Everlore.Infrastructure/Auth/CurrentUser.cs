using System.Security.Claims;
using Everlore.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Everlore.Infrastructure.Auth;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    public Guid? TenantId =>
        Guid.TryParse(User?.FindFirstValue("tenant"), out var id) ? id : null;

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public bool IsSuperAdmin => User?.IsInRole("SuperAdmin") ?? false;
}

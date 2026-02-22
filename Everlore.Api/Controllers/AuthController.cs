using System.Security.Claims;
using System.Threading.RateLimiting;
using Everlore.Api.Models;
using Everlore.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Everlore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request.Email, request.Password);
        return ToResponse(result);
    }

    [HttpPost("select-tenant")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> SelectTenant(SelectTenantRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await authService.SelectTenantAsync(userId, request.TenantId);
        return ToResponse(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request.Email, request.Password, request.FullName);
        return ToResponse(result);
    }

    private ActionResult<AuthResponse> ToResponse(AuthResult result)
    {
        var tenants = result.Tenants?.Select(t =>
            new TenantInfoResponse(t.Id, t.Name, t.Identifier, t.Role)).ToList();

        var response = new AuthResponse(result.Succeeded, result.Token, result.Error, tenants);

        return result.Succeeded ? Ok(response) : BadRequest(response);
    }
}

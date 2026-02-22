using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Everlore.Domain.Tenancy;
using Everlore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Everlore.Infrastructure.Auth;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    CatalogDbContext catalogDb,
    IOptions<JwtSettings> jwtOptions,
    IOptions<RegistrationSettings> registrationOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;
    private readonly RegistrationSettings _registration = registrationOptions.Value;

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return new AuthResult(false, Error: "Invalid email or password.");

        var valid = await userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return new AuthResult(false, Error: "Invalid email or password.");

        var tenants = await GetUserTenantsAsync(user.Id);
        var token = await GenerateTokenAsync(user);

        return new AuthResult(true, Token: token, Tenants: tenants);
    }

    public async Task<AuthResult> SelectTenantAsync(Guid userId, Guid tenantId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new AuthResult(false, Error: "User not found.");

        var tenantUser = await catalogDb.TenantUsers
            .Include(tu => tu.Tenant)
            .FirstOrDefaultAsync(tu => tu.UserId == userId && tu.TenantId == tenantId);

        if (tenantUser is null)
            return new AuthResult(false, Error: "User does not belong to this tenant.");

        var tenants = await GetUserTenantsAsync(userId);
        var token = await GenerateTokenAsync(user, tenantUser.TenantId, tenantUser.Role);

        return new AuthResult(true, Token: token, Tenants: tenants);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string fullName)
    {
        if (_registration.Mode == RegistrationMode.Disabled)
            return new AuthResult(false, Error: "Registration is currently disabled.");

        if (_registration.Mode == RegistrationMode.InviteOnly)
            return new AuthResult(false, Error: "Registration is by invitation only.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new AuthResult(false, Error: errors);
        }

        var token = await GenerateTokenAsync(user);
        return new AuthResult(true, Token: token, Tenants: []);
    }

    private async Task<IReadOnlyList<TenantInfo>> GetUserTenantsAsync(Guid userId)
    {
        return await catalogDb.TenantUsers
            .Where(tu => tu.UserId == userId)
            .Include(tu => tu.Tenant)
            .Select(tu => new TenantInfo(
                tu.Tenant.Id,
                tu.Tenant.Name,
                tu.Tenant.Identifier,
                tu.Role.ToString()))
            .ToListAsync();
    }

    private async Task<string> GenerateTokenAsync(ApplicationUser user, Guid? tenantId = null, TenantRole? role = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("fullName", user.FullName)
        };

        // Include Identity roles (e.g. SuperAdmin)
        var identityRoles = await userManager.GetRolesAsync(user);
        foreach (var identityRole in identityRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, identityRole));
        }

        if (tenantId.HasValue)
        {
            claims.Add(new Claim("tenant", tenantId.Value.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, role!.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

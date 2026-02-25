using System.Security.Cryptography;
using System.Text;
using Everlore.Domain.Tenancy;
using Everlore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Everlore.MigrationService;

public class MigrationWorker(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await catalogDb.Database.MigrateAsync(cancellationToken);

        var devTenant = await SeedDevTenantAsync(catalogDb, cancellationToken);
        await SeedConnectorConfigAsync(catalogDb, devTenant, cancellationToken);
        await SeedSelfHostedDevTenantAsync(catalogDb, cancellationToken);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await SeedRolesAsync(roleManager);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await SeedDevUserAsync(userManager, catalogDb, devTenant, cancellationToken);

        var tenantDb = scope.ServiceProvider.GetRequiredService<EverloreDbContext>();
        await tenantDb.Database.MigrateAsync(cancellationToken);

        hostApplicationLifetime.StopApplication();
    }

    private async Task<Tenant> SeedDevTenantAsync(CatalogDbContext catalogDb, CancellationToken cancellationToken)
    {
        var tenantConnectionString = configuration.GetConnectionString("everloretenantdb")!;

        var devTenant = await catalogDb.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == "dev", cancellationToken);

        if (devTenant is null)
        {
            devTenant = new Tenant
            {
                Name = "Development",
                Identifier = "dev",
                ConnectionString = tenantConnectionString,
                IsActive = true
            };
            catalogDb.Tenants.Add(devTenant);
            await catalogDb.SaveChangesAsync(cancellationToken);
        }
        else if (devTenant.ConnectionString != tenantConnectionString)
        {
            devTenant.ConnectionString = tenantConnectionString;
            await catalogDb.SaveChangesAsync(cancellationToken);
        }

        return devTenant;
    }

    private static async Task SeedSelfHostedDevTenantAsync(CatalogDbContext catalogDb, CancellationToken cancellationToken)
    {
        const string identifier = "dev-selfhosted";
        const string devGatewayKey = "dev-gateway-key";

        var tenant = await catalogDb.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == identifier, cancellationToken);

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Name = "Development (Self-Hosted)",
                Identifier = identifier,
                HostingMode = HostingMode.SelfHosted,
                IsActive = true
            };
            catalogDb.Tenants.Add(tenant);
            await catalogDb.SaveChangesAsync(cancellationToken);
        }
        else if (tenant.HostingMode != HostingMode.SelfHosted)
        {
            tenant.HostingMode = HostingMode.SelfHosted;
            tenant.ConnectionString = null;
            await catalogDb.SaveChangesAsync(cancellationToken);
        }

        // Seed gateway API key (pre-hashed so the agent can authenticate on startup)
        var keyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(devGatewayKey)));
        var existingKey = await catalogDb.GatewayApiKeys
            .AnyAsync(k => k.TenantId == tenant.Id && k.KeyHash == keyHash, cancellationToken);

        if (!existingKey)
        {
            catalogDb.GatewayApiKeys.Add(new GatewayApiKey
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Dev Gateway Key",
                KeyHash = keyHash,
                KeyPrefix = devGatewayKey[..Math.Min(16, devGatewayKey.Length)]
            });
            await catalogDb.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedConnectorConfigAsync(CatalogDbContext catalogDb, Tenant devTenant, CancellationToken cancellationToken)
    {
        if (!await catalogDb.ConnectorConfigurations.AnyAsync(
                c => c.TenantId == devTenant.Id && c.ConnectorType == "seed", cancellationToken))
        {
            catalogDb.ConnectorConfigurations.Add(new ConnectorConfiguration
            {
                TenantId = devTenant.Id,
                ConnectorType = "seed",
                IsEnabled = true,
                Settings = null
            });
            await catalogDb.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        const string superAdminRole = "SuperAdmin";
        if (!await roleManager.RoleExistsAsync(superAdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid> { Name = superAdminRole });
        }
    }

    private static async Task SeedDevUserAsync(
        UserManager<ApplicationUser> userManager,
        CatalogDbContext catalogDb,
        Tenant devTenant,
        CancellationToken cancellationToken)
    {
        const string devEmail = "admin@everlore.dev";
        const string devPassword = "Admin123!";
        const string devFullName = "Dev Admin";

        var existingUser = await userManager.FindByEmailAsync(devEmail);
        if (existingUser is not null)
        {
            if (!await userManager.IsInRoleAsync(existingUser, "SuperAdmin"))
                await userManager.AddToRoleAsync(existingUser, "SuperAdmin");

            // Ensure TenantUser link exists
            var hasLink = await catalogDb.TenantUsers
                .AnyAsync(tu => tu.UserId == existingUser.Id && tu.TenantId == devTenant.Id, cancellationToken);

            if (!hasLink)
            {
                catalogDb.TenantUsers.Add(new TenantUser
                {
                    Id = Guid.NewGuid(),
                    UserId = existingUser.Id,
                    TenantId = devTenant.Id,
                    Role = TenantRole.Admin,
                    CreatedAt = DateTime.UtcNow
                });
                await catalogDb.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = devEmail,
            Email = devEmail,
            FullName = devFullName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, devPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create dev user: {errors}");
        }

        await userManager.AddToRoleAsync(user, "SuperAdmin");

        catalogDb.TenantUsers.Add(new TenantUser
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = devTenant.Id,
            Role = TenantRole.Admin,
            CreatedAt = DateTime.UtcNow
        });
        await catalogDb.SaveChangesAsync(cancellationToken);
    }
}

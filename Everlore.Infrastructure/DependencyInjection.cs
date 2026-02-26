using Everlore.Application.Common.Interfaces;
using Everlore.Domain.AccountsPayable;
using Everlore.Domain.AccountsReceivable;
using Everlore.Domain.Common;
using Everlore.Domain.Inventory;
using Everlore.Domain.Sales;
using Everlore.Domain.Shipping;
using Everlore.Domain.Tenancy;
using Everlore.Infrastructure.Auth;
using Everlore.Infrastructure.Persistence;
using Everlore.Infrastructure.Security;
using Everlore.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Everlore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<TenantConnectionResolver>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICatalogDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddSingleton<IEncryptionService, DataProtectionEncryptionService>();

        // Generic repositories (used by CrudController<T>)
        // Concrete Repository<T> registered for direct resolution and gateway decorator wrapping
        services.AddScoped<Repository<Vendor>>();
        services.AddScoped<Repository<Bill>>();
        services.AddScoped<Repository<Customer>>();
        services.AddScoped<Repository<Invoice>>();
        services.AddScoped<Repository<Product>>();
        services.AddScoped<Repository<Warehouse>>();
        services.AddScoped<Repository<SalesOrder>>();
        services.AddScoped<Repository<Carrier>>();
        services.AddScoped<Repository<Shipment>>();
        services.AddScoped<IRepository<Vendor>>(sp => sp.GetRequiredService<Repository<Vendor>>());
        services.AddScoped<IRepository<Bill>>(sp => sp.GetRequiredService<Repository<Bill>>());
        services.AddScoped<IRepository<Customer>>(sp => sp.GetRequiredService<Repository<Customer>>());
        services.AddScoped<IRepository<Invoice>>(sp => sp.GetRequiredService<Repository<Invoice>>());
        services.AddScoped<IRepository<Product>>(sp => sp.GetRequiredService<Repository<Product>>());
        services.AddScoped<IRepository<Warehouse>>(sp => sp.GetRequiredService<Repository<Warehouse>>());
        services.AddScoped<IRepository<SalesOrder>>(sp => sp.GetRequiredService<Repository<SalesOrder>>());
        services.AddScoped<IRepository<Carrier>>(sp => sp.GetRequiredService<Repository<Carrier>>());
        services.AddScoped<IRepository<Shipment>>(sp => sp.GetRequiredService<Repository<Shipment>>());

        return services;
    }

    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<CatalogDbContext>();

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}

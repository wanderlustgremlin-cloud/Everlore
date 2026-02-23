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

        // Generic repositories (used by CrudController<T>)
        services.AddScoped<IRepository<Vendor>, Repository<Vendor>>();
        services.AddScoped<IRepository<Bill>, Repository<Bill>>();
        services.AddScoped<IRepository<Customer>, Repository<Customer>>();
        services.AddScoped<IRepository<Invoice>, Repository<Invoice>>();
        services.AddScoped<IRepository<Product>, Repository<Product>>();
        services.AddScoped<IRepository<Warehouse>, Repository<Warehouse>>();
        services.AddScoped<IRepository<SalesOrder>, Repository<SalesOrder>>();
        services.AddScoped<IRepository<Carrier>, Repository<Carrier>>();
        services.AddScoped<IRepository<Shipment>, Repository<Shipment>>();

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

using Everlore.Domain.AccountsPayable;
using Everlore.Domain.AccountsReceivable;
using Everlore.Domain.Inventory;
using Everlore.Domain.Sales;
using Everlore.Domain.Shipping;
using Everlore.Domain.Tenancy;
using Everlore.Infrastructure.Auth;
using Everlore.Infrastructure.Persistence;
using Everlore.Infrastructure.Persistence.Repositories;
using Everlore.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Everlore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<TenantConnectionResolver>();

        // Repositories
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IBillRepository, BillRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<ICarrierRepository, CarrierRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();

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

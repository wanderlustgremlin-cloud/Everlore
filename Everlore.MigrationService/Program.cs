using Everlore.Domain.Tenancy;
using Everlore.Infrastructure.Persistence;
using Everlore.MigrationService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var catalogConnectionString = builder.Configuration.GetConnectionString("everloredb")
    ?? throw new InvalidOperationException("Connection string 'everloredb' not found.");

var tenantConnectionString = builder.Configuration.GetConnectionString("everloretenantdb")
    ?? throw new InvalidOperationException("Connection string 'everloretenantdb' not found.");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(catalogConnectionString, o =>
        o.MigrationsAssembly("Everlore.Infrastructure.Postgres")));

builder.Services.AddDbContext<EverloreDbContext>(options =>
    options.UseNpgsql(tenantConnectionString, o =>
        o.MigrationsAssembly("Everlore.Infrastructure.Postgres")));

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<CatalogDbContext>();

builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();
host.Run();

using Everlore.Application.Common.Interfaces;
using Everlore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Everlore.Infrastructure.Postgres.Tenancy;

public class PostgresTenantDatabaseProvisioner(
    IConfiguration configuration,
    ILogger<PostgresTenantDatabaseProvisioner> logger) : ITenantDatabaseProvisioner
{
    public async Task<string> ProvisionAsync(string tenantIdentifier, CancellationToken ct = default)
    {
        var catalogConnectionString = configuration.GetConnectionString("everloredb")
            ?? throw new InvalidOperationException("Connection string 'everloredb' not found.");

        var databaseName = $"everlore_tenant_{tenantIdentifier}";

        var csBuilder = new NpgsqlConnectionStringBuilder(catalogConnectionString)
        {
            Database = databaseName
        };

        var tenantConnectionString = csBuilder.ConnectionString;

        // Connect to the catalog server to create the database
        await using var adminConnection = new NpgsqlConnection(catalogConnectionString);
        await adminConnection.OpenAsync(ct);

        // Check if DB already exists
        await using var checkCmd = adminConnection.CreateCommand();
        checkCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbname";
        checkCmd.Parameters.AddWithValue("dbname", databaseName);
        var exists = await checkCmd.ExecuteScalarAsync(ct) is not null;

        if (!exists)
        {
            logger.LogInformation("Provisioning tenant database: {DatabaseName}", databaseName);

            // CREATE DATABASE doesn't support parameters, but the name is derived from
            // a validated tenant identifier, not raw user input. Quote it for safety.
            await using var createCmd = adminConnection.CreateCommand();
            createCmd.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\"";
            await createCmd.ExecuteNonQueryAsync(ct);
        }

        // Run migrations against the new database
        var optionsBuilder = new DbContextOptionsBuilder<EverloreDbContext>();
        optionsBuilder.UseNpgsql(tenantConnectionString, o =>
            o.MigrationsAssembly("Everlore.Infrastructure.Postgres"));

        await using var tenantDb = new EverloreDbContext(optionsBuilder.Options);
        await tenantDb.Database.MigrateAsync(ct);

        logger.LogInformation("Tenant database provisioned and migrated: {DatabaseName}", databaseName);

        return tenantConnectionString;
    }
}

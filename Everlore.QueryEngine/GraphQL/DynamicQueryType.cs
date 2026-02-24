using Everlore.Application.Common.Interfaces;
using Everlore.QueryEngine.Schema;
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Everlore.QueryEngine.GraphQL;

public class DynamicQueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Query");

        // The explore endpoint: query a specific table from a data source
        descriptor.Field("explore")
            .Argument("dataSourceId", a => a.Type<NonNullType<UuidType>>())
            .Argument("schemaName", a => a.Type<StringType>())
            .Argument("tableName", a => a.Type<NonNullType<StringType>>())
            .Argument("first", a => a.Type<IntType>())
            .Type<ListType<ObjectType<DynamicRowType>>>()
            .Resolve(async ctx =>
            {
                var dataSourceId = ctx.ArgumentValue<Guid>("dataSourceId");
                var schemaName = ctx.ArgumentValue<string?>("schemaName");
                var tableName = ctx.ArgumentValue<string>("tableName");

                var catalogDb = ctx.Service<ICatalogDbContext>();
                var currentUser = ctx.Service<ICurrentUser>();
                var schemaService = ctx.Service<Application.Common.Interfaces.ISchemaService>();
                var resolver = ctx.Service<DynamicQueryResolver>();

                var tenantId = currentUser.TenantId
                    ?? throw new GraphQLException("Tenant context required.");

                var dataSource = await catalogDb.DataSources
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ds => ds.Id == dataSourceId && ds.TenantId == tenantId)
                    ?? throw new GraphQLException($"Data source '{dataSourceId}' not found.");

                var schemaObj = await schemaService.GetSchemaAsync(dataSourceId, false);
                var schema = (DiscoveredSchema)schemaObj;

                var table = schema.Tables.FirstOrDefault(t =>
                    t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)
                    && (schemaName is null || t.SchemaName.Equals(schemaName, StringComparison.OrdinalIgnoreCase)))
                    ?? throw new GraphQLException($"Table '{tableName}' not found in data source schema.");

                return await resolver.ResolveAsync(ctx, dataSource, table);
            });

        // Schema introspection: list tables for a data source
        descriptor.Field("dataSourceSchema")
            .Argument("dataSourceId", a => a.Type<NonNullType<UuidType>>())
            .Type<ObjectType<SchemaInfoType>>()
            .Resolve(async ctx =>
            {
                var dataSourceId = ctx.ArgumentValue<Guid>("dataSourceId");
                var currentUser = ctx.Service<ICurrentUser>();
                var catalogDb = ctx.Service<ICatalogDbContext>();
                var schemaService = ctx.Service<Application.Common.Interfaces.ISchemaService>();

                var tenantId = currentUser.TenantId
                    ?? throw new GraphQLException("Tenant context required.");

                var exists = await catalogDb.DataSources
                    .AnyAsync(ds => ds.Id == dataSourceId && ds.TenantId == tenantId);

                if (!exists)
                    throw new GraphQLException($"Data source '{dataSourceId}' not found.");

                return await schemaService.GetSchemaAsync(dataSourceId, false);
            });
    }
}

public class DynamicRowType : ObjectType<Dictionary<string, object?>>
{
    protected override void Configure(IObjectTypeDescriptor<Dictionary<string, object?>> descriptor)
    {
        descriptor.Name("DynamicRow");
        // Dynamic rows are returned as JSON objects
    }
}

public class SchemaInfoType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("DataSourceSchemaInfo");
    }
}

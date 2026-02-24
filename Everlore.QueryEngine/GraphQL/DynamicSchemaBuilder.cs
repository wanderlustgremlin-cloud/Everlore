using Everlore.QueryEngine.Schema;
using HotChocolate.Language;
using HotChocolate.Types;

namespace Everlore.QueryEngine.GraphQL;

public static class DynamicSchemaBuilder
{
    public static ObjectTypeDefinitionNode BuildTableType(DiscoveredTable table)
    {
        var typeName = SanitizeName($"{table.SchemaName}_{table.TableName}");

        var fields = table.Columns.Select(col =>
        {
            var fieldType = MapToGraphQLType(col.NormalizedType, col.IsNullable);
            return new FieldDefinitionNode(
                null,
                new NameNode(SanitizeName(col.Name)),
                new StringValueNode($"Column {col.Name} ({col.DataType})"),
                Array.Empty<InputValueDefinitionNode>(),
                fieldType,
                Array.Empty<DirectiveNode>());
        }).ToList();

        return new ObjectTypeDefinitionNode(
            null,
            new NameNode(typeName),
            new StringValueNode($"Table {table.SchemaName}.{table.TableName}"),
            Array.Empty<DirectiveNode>(),
            Array.Empty<NamedTypeNode>(),
            fields);
    }

    public static ITypeNode MapToGraphQLType(NormalizedType normalizedType, bool isNullable)
    {
        var namedType = normalizedType switch
        {
            NormalizedType.String => new NamedTypeNode("String"),
            NormalizedType.Integer => new NamedTypeNode("Int"),
            NormalizedType.Decimal => new NamedTypeNode("Float"),
            NormalizedType.DateTime => new NamedTypeNode("DateTime"),
            NormalizedType.Boolean => new NamedTypeNode("Boolean"),
            NormalizedType.Guid => new NamedTypeNode("UUID"),
            _ => new NamedTypeNode("String")
        };

        return isNullable ? namedType : new NonNullTypeNode(namedType);
    }

    private static string SanitizeName(string name)
    {
        // GraphQL names must match [_A-Za-z][_0-9A-Za-z]*
        var sanitized = new string(name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
        if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
            sanitized = "_" + sanitized;
        return sanitized;
    }
}

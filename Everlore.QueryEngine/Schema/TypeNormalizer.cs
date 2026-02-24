using Everlore.Domain.Reporting;

namespace Everlore.QueryEngine.Schema;

public enum NormalizedType
{
    String,
    Integer,
    Decimal,
    DateTime,
    Boolean,
    Guid,
    Other
}

public static class TypeNormalizer
{
    public static NormalizedType Normalize(string dataType, DataSourceType sourceType)
    {
        var lower = dataType.ToLowerInvariant().Trim();

        // Strip length/precision info: varchar(255) → varchar, numeric(10,2) → numeric
        var parenIndex = lower.IndexOf('(');
        if (parenIndex > 0)
            lower = lower[..parenIndex];

        return lower switch
        {
            // String types
            "text" or "varchar" or "character varying" or "char" or "character"
                or "nvarchar" or "nchar" or "ntext" or "longtext" or "mediumtext"
                or "tinytext" or "citext" or "name" => NormalizedType.String,

            // Integer types
            "integer" or "int" or "int4" or "int8" or "int2"
                or "smallint" or "bigint" or "tinyint" or "serial" or "bigserial"
                or "mediumint" => NormalizedType.Integer,

            // Decimal types
            "numeric" or "decimal" or "real" or "double precision" or "float"
                or "float4" or "float8" or "money" or "smallmoney"
                or "double" => NormalizedType.Decimal,

            // DateTime types
            "timestamp" or "timestamp without time zone" or "timestamp with time zone"
                or "timestamptz" or "date" or "datetime" or "datetime2" or "smalldatetime"
                or "datetimeoffset" or "time" or "time without time zone"
                or "time with time zone" => NormalizedType.DateTime,

            // Boolean types
            "boolean" or "bool" or "bit" or "tinyint(1)" => NormalizedType.Boolean,

            // GUID types
            "uuid" or "uniqueidentifier" => NormalizedType.Guid,

            _ => NormalizedType.Other
        };
    }
}

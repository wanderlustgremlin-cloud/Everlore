using Dapper;
using Everlore.QueryEngine.Query;

namespace Everlore.QueryEngine.Translation;

public interface ISqlTranslator
{
    (string Sql, DynamicParameters Parameters) Translate(QueryDefinition query, HashSet<string> validColumns);
}

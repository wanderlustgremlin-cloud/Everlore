namespace Everlore.Application.Common.Interfaces;

public interface ISchemaService
{
    Task<object> GetSchemaAsync(Guid dataSourceId, bool forceRefresh = false, CancellationToken ct = default);
}

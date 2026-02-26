namespace Everlore.Gateway.Contracts.Messages;

public record GatewayCrudRequest(
    string RequestId,
    string EntityType,
    CrudOperation Operation,
    Guid? EntityId,
    string? EntityJson,
    string? PaginationJson,
    string? After);

public enum CrudOperation
{
    GetAll,
    GetById,
    Create,
    Update,
    Delete
}

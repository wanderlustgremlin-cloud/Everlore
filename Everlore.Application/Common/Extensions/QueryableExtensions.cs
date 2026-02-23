using System.Globalization;
using System.Linq.Expressions;
using Everlore.Application.Common.Models;
using Everlore.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationQuery pagination,
        IDictionary<string, string>? filters = null,
        CancellationToken ct = default) where T : class
    {
        query = query.ApplyFilters(filters);

        var totalCount = await query.CountAsync(ct);

        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
        {
            query = query.ApplySort(pagination.SortBy, pagination.SortDir);
        }

        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, totalCount, pagination.Page, pagination.PageSize);
    }

    public static async Task<CursorPagedResult<T>> ToCursorPagedResultAsync<T>(
        this IQueryable<T> query,
        CursorPaginationQuery pagination,
        IDictionary<string, string>? filters = null,
        CancellationToken ct = default) where T : BaseEntity
    {
        query = query.ApplyFilters(filters);

        var totalCount = await query.CountAsync(ct);

        var sortBy = pagination.SortBy ?? "Id";
        var sortDir = pagination.SortDir ?? "asc";
        var descending = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(pagination.After))
        {
            var (sortValue, id) = Cursor.Decode(pagination.After);
            query = ApplyCursorWhere<T>(query, sortBy, sortValue, id, descending);
        }

        query = query.ApplySort(sortBy, sortDir);

        // Secondary sort by Id for deterministic ordering
        if (!sortBy.Equals("Id", StringComparison.OrdinalIgnoreCase))
        {
            query = descending
                ? ((IOrderedQueryable<T>)query).ThenByDescending(e => e.Id)
                : ((IOrderedQueryable<T>)query).ThenBy(e => e.Id);
        }

        var items = await query
            .Take(pagination.PageSize + 1)
            .ToListAsync(ct);

        var hasNextPage = items.Count > pagination.PageSize;
        if (hasNextPage)
            items.RemoveAt(items.Count - 1);

        string? startCursor = null;
        string? endCursor = null;

        if (items.Count > 0)
        {
            startCursor = EncodeCursor(items[0], sortBy);
            endCursor = EncodeCursor(items[^1], sortBy);
        }

        return new CursorPagedResult<T>(
            items, totalCount, hasNextPage,
            !string.IsNullOrWhiteSpace(pagination.After),
            startCursor, endCursor);
    }

    private static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IDictionary<string, string>? filters)
    {
        if (filters is null || filters.Count == 0)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var properties = typeof(T).GetProperties();

        foreach (var filter in filters)
        {
            var key = filter.Key;
            var value = filter.Value;

            // Date range: From/To suffixes
            if (key.EndsWith("From", StringComparison.OrdinalIgnoreCase) || key.EndsWith("To", StringComparison.OrdinalIgnoreCase))
            {
                var isFrom = key.EndsWith("From", StringComparison.OrdinalIgnoreCase);
                var propName = isFrom ? key[..^4] : key[..^2];
                var prop = properties.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));

                if (prop is not null && TryParseDateTime(value, prop.PropertyType, out var dateValue))
                {
                    var propertyAccess = Expression.MakeMemberAccess(parameter, prop);
                    Expression dateExpr = Expression.Constant(dateValue, prop.PropertyType);

                    var comparison = isFrom
                        ? Expression.GreaterThanOrEqual(propertyAccess, dateExpr)
                        : Expression.LessThanOrEqual(propertyAccess, dateExpr);

                    var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
                    query = query.Where(lambda);
                }
                continue;
            }

            // Direct property match
            var matchProp = properties.FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (matchProp is null) continue;

            var propType = Nullable.GetUnderlyingType(matchProp.PropertyType) ?? matchProp.PropertyType;
            object? parsedValue = null;

            if (propType == typeof(string))
                parsedValue = value;
            else if (propType == typeof(bool) && bool.TryParse(value, out var boolVal))
                parsedValue = boolVal;
            else if (propType == typeof(Guid) && Guid.TryParse(value, out var guidVal))
                parsedValue = guidVal;
            else if (propType == typeof(int) && int.TryParse(value, out var intVal))
                parsedValue = intVal;
            else if (propType == typeof(decimal) && decimal.TryParse(value, CultureInfo.InvariantCulture, out var decVal))
                parsedValue = decVal;
            else if (propType == typeof(DateTime) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dtVal))
                parsedValue = dtVal.ToUniversalTime();
            else if (propType.IsEnum && Enum.TryParse(propType, value, ignoreCase: true, out var enumVal))
                parsedValue = enumVal;

            if (parsedValue is null) continue;

            var access = Expression.MakeMemberAccess(parameter, matchProp);
            var constant = Expression.Constant(parsedValue, matchProp.PropertyType);
            var equals = Expression.Equal(access, constant);
            var filterLambda = Expression.Lambda<Func<T, bool>>(equals, parameter);
            query = query.Where(filterLambda);
        }

        return query;
    }

    private static bool TryParseDateTime(string value, Type propertyType, out object result)
    {
        var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (underlying == typeof(DateTime) &&
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
        {
            result = dt.ToUniversalTime();
            return true;
        }
        result = default!;
        return false;
    }

    private static IQueryable<T> ApplyCursorWhere<T>(
        IQueryable<T> query, string sortBy, string sortValue, Guid id, bool descending) where T : BaseEntity
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals(sortBy, StringComparison.OrdinalIgnoreCase));

        if (property is null)
            return query;

        var propType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        object? parsedSort = null;

        if (propType == typeof(string))
            parsedSort = sortValue;
        else if (propType == typeof(DateTime) && DateTime.TryParse(sortValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
            parsedSort = dt.ToUniversalTime();
        else if (propType == typeof(Guid) && Guid.TryParse(sortValue, out var g))
            parsedSort = g;
        else if (propType == typeof(int) && int.TryParse(sortValue, out var i))
            parsedSort = i;
        else if (propType == typeof(decimal) && decimal.TryParse(sortValue, CultureInfo.InvariantCulture, out var d))
            parsedSort = d;

        if (parsedSort is null)
            return query;

        var propAccess = Expression.MakeMemberAccess(parameter, property);
        var sortConstant = Expression.Constant(parsedSort, property.PropertyType);
        var idAccess = Expression.MakeMemberAccess(parameter, typeof(T).GetProperty(nameof(BaseEntity.Id))!);
        var idConstant = Expression.Constant(id);

        // (sortProp > cursor) OR (sortProp == cursor AND Id > cursorId)
        var greaterThan = descending
            ? Expression.LessThan(propAccess, sortConstant)
            : Expression.GreaterThan(propAccess, sortConstant);

        var equalSort = Expression.Equal(propAccess, sortConstant);
        var gtId = descending
            ? Expression.LessThan(idAccess, idConstant)
            : Expression.GreaterThan(idAccess, idConstant);

        var combined = Expression.OrElse(greaterThan, Expression.AndAlso(equalSort, gtId));
        var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);

        return query.Where(lambda);
    }

    private static string EncodeCursor<T>(T entity, string sortBy) where T : BaseEntity
    {
        var property = typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals(sortBy, StringComparison.OrdinalIgnoreCase));

        var sortValue = property?.GetValue(entity)?.ToString() ?? "";
        return Cursor.Encode(sortValue, entity.Id);
    }

    private static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string sortBy, string sortDir)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var property = typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals(sortBy, StringComparison.OrdinalIgnoreCase));

        if (property is null)
            return query;

        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var orderByExpression = Expression.Lambda(propertyAccess, parameter);

        var methodName = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? "OrderByDescending"
            : "OrderBy";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(T), property.PropertyType],
            query.Expression,
            Expression.Quote(orderByExpression));

        return query.Provider.CreateQuery<T>(resultExpression);
    }
}

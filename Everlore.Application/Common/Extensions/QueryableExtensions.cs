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
        CancellationToken ct = default) where T : class
    {
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

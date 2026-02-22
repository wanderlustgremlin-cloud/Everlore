using System.Net;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Everlore.Infrastructure.Tenancy;

namespace Everlore.Api.Middleware;

public class TenantRequiredMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> ExemptPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth",
        "/api/tenants"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) || IsExempt(path))
        {
            await next(context);
            return;
        }

        var tenantProvider = context.RequestServices.GetRequiredService<ITenantProvider>();
        var identifier = tenantProvider.GetTenantIdentifier();

        if (string.IsNullOrWhiteSpace(identifier))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                title = "Tenant Required",
                status = 403,
                detail = "A valid tenant context is required for this request. Use /api/auth/select-tenant to obtain a tenant-scoped token."
            };

            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        await next(context);
    }

    private static bool IsExempt(string path)
    {
        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

namespace Everlore.Api.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    private static readonly string[] SwaggerPrefixes = ["/swagger", "/api/openapi"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isSwagger = Array.Exists(SwaggerPrefixes,
            prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (!isSwagger)
        {
            headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'";
        }

        await next(context);
    }
}

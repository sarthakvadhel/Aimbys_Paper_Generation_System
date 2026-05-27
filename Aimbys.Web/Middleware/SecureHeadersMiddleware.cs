namespace Aimbys.Web.Middleware;

/// <summary>
/// Appends security headers to every HTTP response. Runs as early
/// as possible in the pipeline so even error pages carry the headers.
/// </summary>
public sealed class SecureHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecureHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            // HSTS is handled by app.UseHsts() in non-dev; add unconditionally
            // here with a 1-year max-age to cover dev testing as well.
            headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>
/// Extension method that fluent-registers
/// <see cref="SecureHeadersMiddleware"/> in the request pipeline.
/// </summary>
public static class SecureHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecureHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecureHeadersMiddleware>();
}

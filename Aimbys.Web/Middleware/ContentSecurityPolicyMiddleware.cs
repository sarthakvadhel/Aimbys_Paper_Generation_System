using System.Security.Cryptography;

namespace Aimbys.Web.Middleware;

/// <summary>
/// Sets a strict Content-Security-Policy header. Inline scripts are
/// blocked; nonce-based exceptions allow the Chart.js init scripts
/// in SystemHealth and similar views. The nonce is stored in
/// HttpContext.Items["CspNonce"] so Razor views can emit it.
/// </summary>
public sealed class ContentSecurityPolicyMiddleware
{
    private readonly RequestDelegate _next;

    // CDN origins for allowed external scripts/styles
    private const string TinyMceCdn = "https://cdn.tiny.cloud";
    private const string MonacoCdn = "https://cdnjs.cloudflare.com";
    private const string ChartJsCdn = "https://cdn.jsdelivr.net";
    private const string KatexCdn = "https://cdnjs.cloudflare.com";
    private const string BootstrapCdn = "https://cdn.jsdelivr.net";
    private const string FontCdn = "https://fonts.googleapis.com https://fonts.gstatic.com";

    public ContentSecurityPolicyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a per-request nonce for inline scripts
        var nonceBytes = RandomNumberGenerator.GetBytes(16);
        var nonce = Convert.ToBase64String(nonceBytes);
        context.Items["CspNonce"] = nonce;

        context.Response.OnStarting(() =>
        {
            // Don't set CSP on redirects or non-HTML responses
            if (context.Response.StatusCode >= 300 && context.Response.StatusCode < 400)
                return Task.CompletedTask;

            var policy = string.Join("; ",
                "default-src 'self'",
                $"script-src 'self' 'nonce-{nonce}' {TinyMceCdn} {MonacoCdn} {ChartJsCdn}",
                $"style-src 'self' 'unsafe-inline' {KatexCdn} {BootstrapCdn} {FontCdn}",
                $"font-src 'self' {FontCdn} {KatexCdn}",
                $"img-src 'self' data: blob:",
                $"connect-src 'self' {TinyMceCdn}",
                "frame-ancestors 'none'",
                "base-uri 'self'",
                "form-action 'self'");

            context.Response.Headers.TryAdd("Content-Security-Policy", policy);
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>
/// Extension method that fluent-registers
/// <see cref="ContentSecurityPolicyMiddleware"/> in the request pipeline.
/// </summary>
public static class ContentSecurityPolicyMiddlewareExtensions
{
    public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder app)
        => app.UseMiddleware<ContentSecurityPolicyMiddleware>();
}

using System.Diagnostics;
using Aimbys.Application.SystemHealth;
using Microsoft.AspNetCore.Http;

namespace Aimbys.Infrastructure.SystemHealth;

/// <summary>
/// ASP.NET Core middleware that records every completed request into
/// the singleton <see cref="IRequestMetricsCollector"/>. Wired up in
/// <c>Program.cs</c> immediately after <c>UseRouting</c> so it sees
/// every endpoint, including auth-protected ones.
/// </summary>
public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestMetricsCollector _collector;

    public RequestMetricsMiddleware(RequestDelegate next, IRequestMetricsCollector collector)
    {
        _next = next;
        _collector = collector;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _collector.Record(context.Response.StatusCode, sw.ElapsedMilliseconds);
        }
    }
}

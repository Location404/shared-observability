namespace ObservabilitySdk.Observability;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Diagnostics;

public class ObservabilityMiddleware(RequestDelegate next, ObservabilityConfiguration config)
{
    private readonly RequestDelegate _next = next;
    private readonly ObservabilityConfiguration _config = config;
    private static readonly ActivitySource ActivitySource = new("Location404.Observability");

    public async Task InvokeAsync(HttpContext context)
    {
        using var activity = ActivitySource.StartActivity($"{context.Request.Method} {context.Request.Path}");
        
        // Add custom tags
        activity?.SetTag("http.method", context.Request.Method);
        activity?.SetTag("http.url", context.Request.GetDisplayUrl());
        activity?.SetTag("user.id", context.User.Identity?.Name);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
            
            activity?.SetTag("http.status_code", context.Response.StatusCode);
            activity?.SetStatus(context.Response.StatusCode >= 400 ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            activity?.SetTag("http.duration_ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
using System.Diagnostics;

namespace App.Api.Middleware;

public sealed class RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger)
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string TraceIdHeader = "X-Trace-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var incomingId) && !string.IsNullOrWhiteSpace(incomingId)
            ? incomingId.ToString()
            : Guid.NewGuid().ToString("N");

        var currentActivity = Activity.Current;
        var traceId = currentActivity?.TraceId.ToString() ?? ActivityTraceId.CreateRandom().ToString();
        var spanId = currentActivity?.SpanId.ToString() ?? string.Empty;

        currentActivity?.SetTag("correlation.id", correlationId);

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd(CorrelationIdHeader, correlationId);
            headers.TryAdd(TraceIdHeader, traceId);
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
            ["SpanId"] = spanId,
            ["RequestMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.Value ?? string.Empty
        }))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await next(context);
            }
            finally
            {
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                if (statusCode >= 500)
                {
                    logger.LogError("HTTP {Method} {Path} failed with status {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
                        context.Request.Method, context.Request.Path, statusCode, elapsedMs, correlationId);
                }
                else if (statusCode >= 400)
                {
                    logger.LogWarning("HTTP {Method} {Path} completed with client error {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
                        context.Request.Method, context.Request.Path, statusCode, elapsedMs, correlationId);
                }
                else
                {
                    logger.LogInformation("HTTP {Method} {Path} completed with status {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
                        context.Request.Method, context.Request.Path, statusCode, elapsedMs, correlationId);
                }
            }
        }
    }
}

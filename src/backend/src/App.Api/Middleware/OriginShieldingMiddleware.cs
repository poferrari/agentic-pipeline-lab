using Microsoft.AspNetCore.Mvc;

namespace App.Api.Middleware;

public sealed class OriginShieldingMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<OriginShieldingMiddleware> logger)
{
    public const string HeaderName = "X-Origin-Verify";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Always allow health probe endpoints through so container orchestrators / cloud hosts can verify liveness/readiness
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        var isShieldingEnabled = configuration.GetValue("OriginShielding:Enabled", false);
        var expectedSecret = configuration["OriginShielding:Secret"];

        if (isShieldingEnabled &&
            !string.IsNullOrWhiteSpace(expectedSecret) &&
            (!context.Request.Headers.TryGetValue(HeaderName, out var receivedSecret) ||
             !string.Equals(receivedSecret.ToString(), expectedSecret, StringComparison.Ordinal)))
        {
            logger.LogWarning(
                "Direct origin access rejected for path {Path} from remote IP {RemoteIp}. Missing or invalid {HeaderName} header.",
                context.Request.Path,
                context.Connection.RemoteIpAddress,
                HeaderName);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Security.DirectOriginAccessForbidden",
                Detail = "Direct access to the origin service is prohibited. Requests must route through the configured edge perimeter.",
                Type = "https://httpstatuses.com/403"
            };

            await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
            return;
        }

        await next(context);
    }
}

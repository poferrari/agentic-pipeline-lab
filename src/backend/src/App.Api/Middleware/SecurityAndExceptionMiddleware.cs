using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace App.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.TryAdd("X-XSS-Protection", "0");
            headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            headers.TryAdd("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self';");
            headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
            if (context.Request.IsHttps)
            {
                headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            }
            return Task.CompletedTask;
        });

        await next(context);
    }
}

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        logger.LogError(exception, "Unhandled exception occurred while processing request {Path}.", httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server.UnexpectedError",
            Detail = "An unexpected error occurred. Please try again later.",
            Type = "https://httpstatuses.com/500"
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}

public sealed class HybridSupabaseOrDemoAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "HybridSupabaseOrDemo";
    public const string DemoHeaderName = "X-Demo-User-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var allowDemoHeader = configuration.GetValue("Auth:AllowDemoHeader", true);
        if (allowDemoHeader &&
            Request.Headers.TryGetValue(DemoHeaderName, out var rawUserId) &&
            Guid.TryParse(rawUserId.FirstOrDefault(), out var parsedUserId) &&
            parsedUserId != Guid.Empty)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, parsedUserId.ToString()),
                new Claim("sub", parsedUserId.ToString()),
                new Claim(ClaimTypes.Role, "authenticated")
            };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

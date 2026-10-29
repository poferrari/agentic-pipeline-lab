using System.Threading.RateLimiting;
using App.Api.Diagnostics;
using App.Api.Endpoints;
using App.Api.Middleware;
using App.Application;
using App.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var otelResource = ResourceBuilder.CreateDefault()
    .AddService(serviceName: MarketPulseDiagnostics.ServiceName, serviceVersion: MarketPulseDiagnostics.ServiceVersion);

Action<OtlpExporterOptions> configureOtlp = opt =>
{
    var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
    {
        opt.Endpoint = new Uri(otlpEndpoint);
    }

    var protocol = builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
    if (string.Equals(protocol, "http/protobuf", StringComparison.OrdinalIgnoreCase))
    {
        opt.Protocol = OtlpExportProtocol.HttpProtobuf;
    }
    else if (string.Equals(protocol, "grpc", StringComparison.OrdinalIgnoreCase))
    {
        opt.Protocol = OtlpExportProtocol.Grpc;
    }

    var headers = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"];
    if (!string.IsNullOrWhiteSpace(headers))
    {
        opt.Headers = headers;
    }
};

builder.Services.Configure<OpenTelemetryLoggerOptions>(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: MarketPulseDiagnostics.ServiceName, serviceVersion: MarketPulseDiagnostics.ServiceVersion))
    .WithTracing(tracing =>
    {
        tracing.AddSource(MarketPulseDiagnostics.ServiceName)
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            });

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(configureOtlp);
        }
        else if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(MarketPulseDiagnostics.ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(configureOtlp);
        }
        else if (builder.Environment.IsDevelopment())
        {
            metrics.AddConsoleExporter();
        }
    })
    .WithLogging(logging =>
    {
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            logging.AddOtlpExporter(configureOtlp);
        }
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

var supabaseUrl = builder.Configuration["Supabase:Url"] ?? "https://example-project.supabase.co";
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "CompositeAuth";
        options.DefaultChallengeScheme = "CompositeAuth";
    })
    .AddPolicyScheme("CompositeAuth", "Supabase JWT or Demo Header", policyOptions =>
    {
        policyOptions.ForwardDefaultSelector = context =>
        {
            if (context.Request.Headers.ContainsKey(HybridSupabaseOrDemoAuthHandler.DemoHeaderName))
            {
                return HybridSupabaseOrDemoAuthHandler.SchemeName;
            }

            return JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddScheme<AuthenticationSchemeOptions, HybridSupabaseOrDemoAuthHandler>(
        HybridSupabaseOrDemoAuthHandler.SchemeName,
        _ => { })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("StrictCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", HybridSupabaseOrDemoAuthHandler.DemoHeaderName, RequestTelemetryMiddleware.CorrelationIdHeader);
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("StrictCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();

app.MapGet("/", () => TypedResults.Ok(new
{
    service = "MarketPulse Lab - Hexagonal DDD API",
    version = "1.0.0",
    architecture = "Hexagonal (Ports & Adapters) + DDD + HybridCache + MassTransit + OpenTelemetry",
    publicApiProvider = "Frankfurter ECB Exchange Rates API (https://api.frankfurter.app)",
    observability = new
    {
        telemetry = "OpenTelemetry (Tracing, Metrics & Structured Logging)",
        serviceName = MarketPulseDiagnostics.ServiceName,
        serviceVersion = MarketPulseDiagnostics.ServiceVersion,
        protocol = "OTLP / W3C TraceContext"
    },
    documentation = "/openapi/v1.json",
    endpoints = EndpointCatalog
})).AllowAnonymous().WithTags("System");

app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();

var endpoints = typeof(Program).Assembly
    .GetTypes()
    .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IEndpoint).IsAssignableFrom(t))
    .Select(Activator.CreateInstance)
    .Cast<IEndpoint>();

foreach (var endpoint in endpoints)
{
    endpoint.MapEndpoint(app);
}

await app.RunAsync();

public partial class Program
{
    private static readonly string[] EndpointCatalog =
    [
        "GET  /api/v1/rates/{baseCurrency}?symbols=BRL,EUR,GBP",
        "POST /api/v1/watchlist (Header: Authorization Bearer JWT or X-Demo-User-Id)",
        "GET  /api/v1/watchlist?page=1&pageSize=20",
        "GET  /api/v1/watchlist/{id}",
        "DELETE /api/v1/watchlist/{id}",
        "GET  /health/live",
        "GET  /health/ready"
    ];

    protected Program()
    {
    }
}

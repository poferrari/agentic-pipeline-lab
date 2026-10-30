using System.Threading.RateLimiting;
using App.Api.Endpoints;
using App.Api.Middleware;
using App.Application;
using App.Application.Common;
using App.Infrastructure;
using App.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2 * 1024 * 1024; // 2 MB limit to mitigate buffer exhaustion DoS
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

Action<OtlpExporterOptions, string> configureOtlp = (opt, signalPath) =>
{
    var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
        ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
    var protocol = builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]
        ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
    var headers = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]
        ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");

    var isHttpProtobuf = string.Equals(protocol, "http/protobuf", StringComparison.OrdinalIgnoreCase);

    if (isHttpProtobuf)
    {
        opt.Protocol = OtlpExportProtocol.HttpProtobuf;
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            var trimmed = otlpEndpoint.TrimEnd('/');
            if (!trimmed.EndsWith(signalPath, StringComparison.OrdinalIgnoreCase))
            {
                opt.Endpoint = new Uri($"{trimmed}/{signalPath}");
            }
            else
            {
                opt.Endpoint = new Uri(trimmed);
            }
        }
    }
    else
    {
        opt.Protocol = OtlpExportProtocol.Grpc;
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            opt.Endpoint = new Uri(otlpEndpoint);
        }
    }

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

var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
    ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: MarketPulseDiagnostics.ServiceName, serviceVersion: MarketPulseDiagnostics.ServiceVersion))
    .WithTracing(tracing =>
    {
        tracing.AddSource(MarketPulseDiagnostics.ServiceName)
            .AddSource("MassTransit")
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            });

        if (hasOtlpEndpoint)
        {
            tracing.AddOtlpExporter(opt => configureOtlp(opt, "v1/traces"));
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

        if (hasOtlpEndpoint)
        {
            metrics.AddOtlpExporter((opt, metricReaderOpt) =>
            {
                configureOtlp(opt, "v1/metrics");
                metricReaderOpt.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
            });
        }
        else if (builder.Environment.IsDevelopment())
        {
            metrics.AddConsoleExporter();
        }
    })
    .WithLogging(logging =>
    {
        if (hasOtlpEndpoint)
        {
            logging.AddOtlpExporter(opt => configureOtlp(opt, "v1/logs"));
        }
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolveClientPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("strict", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolveClientPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
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
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                return false;
            }

            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                       uri.Host.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase) ||
                       allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        })
            .WithMethods("GET", "POST", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", HybridSupabaseOrDemoAuthHandler.DemoHeaderName, RequestTelemetryMiddleware.CorrelationIdHeader);
    });
});

var app = builder.Build();

app.UseCors("StrictCors");
app.UseExceptionHandler();
app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseMiddleware<OriginShieldingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();

app.MapGet("/", () => TypedResults.Ok(new
{
    service = "MarketPulse Lab - Hexagonal DDD API",
    version = "1.0.0",
    architecture = "Hexagonal (Ports & Adapters) + DDD + HybridCache + MassTransit + OpenTelemetry",
    messaging = new
    {
        broker = "RabbitMQ / CloudAMQP (AMQP 0-9-1)",
        topology = "Exchange-to-Exchange with Queue bindings",
        queue = "marketpulse.currency-alerts",
        contracts = new
        {
            wireOut = "CurrencyAlertNotificationEvent (Strict Schema)",
            wireIn = "ICurrencyAlertConsumerContract (Loose Schema)"
        }
    },
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

app.MapEndpoints();

if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Database")))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
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

    private static string ResolveClientPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        if (httpContext.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp) &&
            !string.IsNullOrWhiteSpace(cfIp.FirstOrDefault()))
        {
            return $"cf-ip:{cfIp[0]!.Trim()}";
        }

        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) &&
            !string.IsNullOrWhiteSpace(forwardedFor.FirstOrDefault()))
        {
            var clientIp = forwardedFor[0]!.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                return $"xff-ip:{clientIp}";
            }
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"}";
    }
}

using App.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace App.IntegrationTests.Infrastructure;

public sealed class MarketPulseTestcontainersFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer? _postgresContainer;
    private readonly RabbitMqContainer? _rabbitMqContainer;

    public bool IsDockerEnvironmentActive { get; private set; }
    public string? RabbitMqConnectionString => _rabbitMqContainer?.GetConnectionString();
    public string? DatabaseConnectionString => _postgresContainer?.GetConnectionString();

    public MarketPulseTestcontainersFactory()
    {
        try
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:17-alpine")
                .WithDatabase("marketpulse_test_db")
                .WithUsername("postgres")
                .WithPassword("postgres_test_password")
                .Build();

            _rabbitMqContainer = new RabbitMqBuilder()
                .WithImage("rabbitmq:4-management-alpine")
                .WithUsername("guest")
                .WithPassword("guest")
                .Build();
        }
        catch
        {
            _postgresContainer = null;
            _rabbitMqContainer = null;
        }
    }

    public async Task InitializeAsync()
    {
        if (_postgresContainer is not null && _rabbitMqContainer is not null)
        {
            try
            {
                await _postgresContainer.StartAsync().ConfigureAwait(false);
                await _rabbitMqContainer.StartAsync().ConfigureAwait(false);
                IsDockerEnvironmentActive = true;

                // Ensure the database schema is created BEFORE the WebHost and background workers start
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(_postgresContainer.GetConnectionString());
                await using var initDbContext = new AppDbContext(optionsBuilder.Options);
                await initDbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Testcontainers Warning] Docker fallback to in-memory: {ex.Message}");
                IsDockerEnvironmentActive = false;
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("Auth:AllowDemoHeader", "true");
        builder.UseSetting("Messaging:Retry:Limit", "3");
        builder.UseSetting("Messaging:Retry:InitialIntervalSeconds", "1");
        builder.UseSetting("Messaging:Retry:MaxIntervalSeconds", "2");
        builder.UseSetting("Messaging:Retry:IntervalDeltaSeconds", "1");

        if (_postgresContainer is not null && _rabbitMqContainer is not null)
        {
            builder.UseSetting("ConnectionStrings:Database", _postgresContainer.GetConnectionString());
            builder.UseSetting("ConnectionStrings:RabbitMq", _rabbitMqContainer.GetConnectionString());
        }
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);

        if (_postgresContainer is not null)
        {
            try
            {
                await _postgresContainer.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        if (_rabbitMqContainer is not null)
        {
            try
            {
                await _rabbitMqContainer.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}

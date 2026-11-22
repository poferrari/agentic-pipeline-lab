using App.Infrastructure.Persistence;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace App.IntegrationTests.Infrastructure;

/// <summary>
/// Guards against coverage theater: the integration suite must run on real PostgreSQL and RabbitMQ,
/// never on the in-memory database or the MassTransit loopback bus.
/// </summary>
[Collection(MarketPulseTestGroupDefinition.CollectionName)]
public sealed class RealInfrastructureTests(MarketPulseTestcontainersFactory factory)
{
    [Fact]
    public void Suite_WhenRunning_ShouldUseRealPostgreSqlAndRabbitMq()
    {
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.ProviderName;
        var busAddress = scope.ServiceProvider.GetRequiredService<IBus>().Address;

        factory.IsDockerEnvironmentActive.Should().BeTrue("integration tests must not silently fall back to in-memory");
        provider.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
        busAddress.Scheme.Should().Be("rabbitmq", "the bus must be connected to the RabbitMQ container, not loopback");
    }
}

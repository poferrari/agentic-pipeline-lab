using Xunit;

namespace App.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit Collection Definition to ensure PostgreSQL and RabbitMQ Testcontainers
/// spin up EXACTLY ONCE for the entire test suite execution, share the same state/containers
/// across all integration test classes, and tear down ONLY after all tests complete.
/// </summary>
[CollectionDefinition(CollectionName)]
public sealed class MarketPulseTestGroupDefinition : ICollectionFixture<MarketPulseTestcontainersFactory>
{
    public const string CollectionName = "MarketPulse-Testcontainers-Collection";
}

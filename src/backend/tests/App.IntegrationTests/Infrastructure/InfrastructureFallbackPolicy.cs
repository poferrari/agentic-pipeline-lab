namespace App.IntegrationTests.Infrastructure;

/// <summary>
/// Decides whether integration tests may run without real containers.
/// Failing loudly is the default: silently using the in-memory database and the MassTransit loopback bus
/// makes the suite pass without exercising PostgreSQL, RabbitMQ, the Outbox, or message retry (coverage theater).
/// </summary>
internal static class InfrastructureFallbackPolicy
{
    public const string OptInVariable = "MARKETPULSE_ALLOW_INMEMORY_FALLBACK";

    public static void EnsureRealInfrastructure(Exception? startupFailure, string? optInValue)
    {
        if (startupFailure is null || string.Equals(optInValue, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            "Integration tests require running Docker containers (PostgreSQL and RabbitMQ) but they could not be started. " +
            "Start Docker Desktop and rerun. To knowingly run on in-memory substitutes, set " +
            $"{OptInVariable}=true (semantics of Outbox, retry, and dead-letter are NOT exercised in that mode).",
            startupFailure);
    }
}

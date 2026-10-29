namespace App.Application.MarketPulse.Events;

/// <summary>
/// Wire-In Loose Schema interface.
/// Following Postel's Law (Robustness Principle), consumers bind to this minimal contract
/// so that producers can evolve their wire-out schemas without breaking downstream consumers.
/// </summary>
public interface ICurrencyAlertConsumerContract
{
    Guid EventId { get; }
    Guid WatchItemId { get; }
    Guid OwnerId { get; }
    string BaseCurrency { get; }
    string TargetCurrency { get; }
    decimal TargetRate { get; }
    decimal? CurrentRate { get; }
    decimal? SpreadPercentage { get; }
    bool IsTriggered { get; }
    string? Notes { get; }
    string? CorrelationId { get; }
    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>
/// Wire-Out Strict Schema record.
/// Emitted by the application to the message broker (RabbitMQ / CloudAMQP).
/// Strictly versioned, immutable, and enriched with correlation and causation metadata.
/// </summary>
public sealed record CurrencyAlertNotificationEvent(
    Guid EventId,
    Guid WatchItemId,
    Guid OwnerId,
    string BaseCurrency,
    string TargetCurrency,
    decimal TargetRate,
    decimal? CurrentRate,
    decimal? SpreadPercentage,
    bool IsTriggered,
    string? Notes,
    string? CorrelationId,
    DateTimeOffset OccurredOnUtc,
    string SchemaVersion = "1.0.0") : ICurrencyAlertConsumerContract;


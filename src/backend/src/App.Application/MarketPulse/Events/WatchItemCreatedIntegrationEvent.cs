namespace App.Application.MarketPulse.Events;

public sealed record WatchItemCreatedIntegrationEvent(
    Guid EventId,
    Guid WatchItemId,
    Guid OwnerId,
    string BaseCurrency,
    string TargetCurrency,
    decimal TargetRate,
    decimal? CurrentRate,
    bool IsTriggered,
    DateTimeOffset OccurredOnUtc);

using App.Domain.Common;

namespace App.Domain.MarketPulse.Events;

public sealed record WatchItemCreatedDomainEvent(
    Guid WatchItemId,
    Guid OwnerId,
    string BaseCurrency,
    string TargetCurrency,
    decimal TargetRate,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

public sealed record TargetRateTriggeredDomainEvent(
    Guid WatchItemId,
    Guid OwnerId,
    string BaseCurrency,
    string TargetCurrency,
    decimal TargetRate,
    decimal CurrentRate,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

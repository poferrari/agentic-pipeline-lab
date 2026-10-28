namespace App.Application.MarketPulse.DTOs;

public sealed record ExchangeRatesQuoteDto(
    string BaseCurrency,
    string ReferenceDate,
    string Provider,
    IReadOnlyDictionary<string, decimal> Rates,
    DateTimeOffset FetchedAtUtc);

public sealed record WatchItemDto(
    Guid Id,
    Guid OwnerId,
    string BaseCurrency,
    string TargetCurrency,
    decimal TargetRate,
    decimal? CurrentRate,
    decimal SpreadPercentage,
    bool NotifyWhenAbove,
    bool IsTriggered,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? TriggeredAtUtc);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

using App.Domain.Common;
using App.Domain.MarketPulse.Errors;
using App.Domain.MarketPulse.Events;
using App.Domain.MarketPulse.ValueObjects;

namespace App.Domain.MarketPulse.Entities;

public sealed class CurrencyWatchItem : AggregateRoot<Guid>
{
    public const int MaxNotesLength = 250;

    private CurrencyWatchItem()
    {
    }

    private CurrencyWatchItem(
        Guid id,
        Guid ownerId,
        CurrencyCode baseCurrency,
        CurrencyCode targetCurrency,
        TargetRate targetRate,
        bool notifyWhenAbove,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OwnerId = ownerId;
        BaseCurrency = baseCurrency;
        TargetCurrency = targetCurrency;
        TargetRate = targetRate;
        NotifyWhenAbove = notifyWhenAbove;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid OwnerId { get; private init; }
    public CurrencyCode BaseCurrency { get; private init; }
    public CurrencyCode TargetCurrency { get; private init; }
    public TargetRate TargetRate { get; private init; }
    public bool NotifyWhenAbove { get; private init; }
    public string? Notes { get; private set; }
    public decimal? LastKnownRate { get; private set; }
    public bool IsTriggered { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private init; }
    public DateTimeOffset? TriggeredAtUtc { get; private set; }

    /// <summary>
    /// Factory creating a CurrencyWatchItem from strongly-typed domain value objects.
    /// Follows Object Calisthenics: wraps primitives and enforces domain invariants.
    /// </summary>
    public static Result<CurrencyWatchItem> Create(
        Guid ownerId,
        CurrencyCode baseCurrency,
        CurrencyCode targetCurrency,
        TargetRate targetRate,
        bool notifyWhenAbove,
        string? notes,
        DateTimeOffset? createdAtUtc = null)
    {
        if (ownerId == Guid.Empty)
        {
            return Result.Failure<CurrencyWatchItem>(MarketPulseErrors.InvalidOwner);
        }

        if (baseCurrency == targetCurrency)
        {
            return Result.Failure<CurrencyWatchItem>(MarketPulseErrors.SameCurrencyNotAllowed);
        }

        var sanitizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (sanitizedNotes is not null && sanitizedNotes.Length > MaxNotesLength)
        {
            return Result.Failure<CurrencyWatchItem>(MarketPulseErrors.NotesTooLong);
        }

        var timestamp = createdAtUtc ?? DateTimeOffset.UtcNow;
        var item = new CurrencyWatchItem(
            Guid.CreateVersion7(),
            ownerId,
            baseCurrency,
            targetCurrency,
            targetRate,
            notifyWhenAbove,
            sanitizedNotes,
            timestamp);

        item.RaiseDomainEvent(new WatchItemCreatedDomainEvent(
            item.Id,
            item.OwnerId,
            item.BaseCurrency.Value,
            item.TargetCurrency.Value,
            item.TargetRate.Value,
            timestamp));

        return Result.Success(item);
    }

    /// <summary>
    /// Convenience factory accepting raw primitive types, validating and delegating to the strongly-typed overload.
    /// </summary>
    public static Result<CurrencyWatchItem> Create(
        Guid ownerId,
        string? baseCurrencyRaw,
        string? targetCurrencyRaw,
        decimal targetRateRaw,
        bool notifyWhenAbove,
        string? notes,
        DateTimeOffset? createdAtUtc = null)
    {
        var baseResult = CurrencyCode.Create(baseCurrencyRaw);
        if (baseResult.IsFailure)
        {
            return Result.Failure<CurrencyWatchItem>(baseResult.Error);
        }

        var targetResult = CurrencyCode.Create(targetCurrencyRaw);
        if (targetResult.IsFailure)
        {
            return Result.Failure<CurrencyWatchItem>(targetResult.Error);
        }

        var rateResult = TargetRate.Create(targetRateRaw);
        if (rateResult.IsFailure)
        {
            return Result.Failure<CurrencyWatchItem>(rateResult.Error);
        }

        return Create(
            ownerId,
            baseResult.Value,
            targetResult.Value,
            rateResult.Value,
            notifyWhenAbove,
            notes,
            createdAtUtc);
    }

    /// <summary>
    /// Pure domain predicate determining whether a given market rate satisfies the alert threshold.
    /// Adheres to Tell, Don't Ask.
    /// </summary>
    public bool IsTargetRateMet(decimal marketRate) =>
        NotifyWhenAbove
            ? marketRate >= TargetRate.Value
            : marketRate <= TargetRate.Value;

    public Result EvaluateMarketRate(decimal currentMarketRate, DateTimeOffset? evaluatedAtUtc = null)
    {
        if (currentMarketRate <= 0m)
        {
            return Result.Failure(MarketPulseErrors.InvalidTargetRate);
        }

        LastKnownRate = decimal.Round(currentMarketRate, 6, MidpointRounding.AwayFromZero);

        if (IsTargetRateMet(LastKnownRate.Value) && !IsTriggered)
        {
            var timestamp = evaluatedAtUtc ?? DateTimeOffset.UtcNow;
            IsTriggered = true;
            TriggeredAtUtc = timestamp;

            RaiseDomainEvent(new TargetRateTriggeredDomainEvent(
                Id,
                OwnerId,
                BaseCurrency.Value,
                TargetCurrency.Value,
                TargetRate.Value,
                LastKnownRate.Value,
                timestamp));
        }

        return Result.Success();
    }

    public decimal CalculateSpreadPercentage(decimal currentMarketRate)
    {
        if (TargetRate.Value == 0m || currentMarketRate <= 0m)
        {
            return 0m;
        }

        var spread = ((currentMarketRate - TargetRate.Value) / TargetRate.Value) * 100m;
        return decimal.Round(spread, 2, MidpointRounding.AwayFromZero);
    }
}

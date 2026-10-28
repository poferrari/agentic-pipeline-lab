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

    public static Result<CurrencyWatchItem> Create(
        Guid ownerId,
        string? baseCurrencyRaw,
        string? targetCurrencyRaw,
        decimal targetRateRaw,
        bool notifyWhenAbove,
        string? notes,
        DateTimeOffset? createdAtUtc = null)
    {
        if (ownerId == Guid.Empty)
        {
            return Result.Failure<CurrencyWatchItem>(MarketPulseErrors.InvalidOwner);
        }

        var baseCurrencyResult = CurrencyCode.Create(baseCurrencyRaw);
        if (baseCurrencyResult.IsFailure)
        {
            return Result.Failure<CurrencyWatchItem>(baseCurrencyResult.Error);
        }

        var targetCurrencyResult = CurrencyCode.Create(targetCurrencyRaw);
        if (targetCurrencyResult.IsFailure)
        {
            return Result.Failure<CurrencyWatchItem>(targetCurrencyResult.Error);
        }

        if (baseCurrencyResult.Value == targetCurrencyResult.Value)
        {
            return Result.Failure<CurrencyWatchItem>(MarketPulseErrors.SameCurrencyNotAllowed);
        }

        var targetRateResult = TargetRate.Create(targetRateRaw);
        if (targetRateResult.IsFailure)
        {
            return Result.Failure<CurrencyWatchItem>(targetRateResult.Error);
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
            baseCurrencyResult.Value,
            targetCurrencyResult.Value,
            targetRateResult.Value,
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

    public Result EvaluateMarketRate(decimal currentMarketRate, DateTimeOffset? evaluatedAtUtc = null)
    {
        if (currentMarketRate <= 0m)
        {
            return Result.Failure(MarketPulseErrors.InvalidTargetRate);
        }

        LastKnownRate = decimal.Round(currentMarketRate, 6, MidpointRounding.AwayFromZero);

        var thresholdMet = NotifyWhenAbove
            ? LastKnownRate.Value >= TargetRate.Value
            : LastKnownRate.Value <= TargetRate.Value;

        if (thresholdMet && !IsTriggered)
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

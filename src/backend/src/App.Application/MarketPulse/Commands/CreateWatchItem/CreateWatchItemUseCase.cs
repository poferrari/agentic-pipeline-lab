using App.Application.Abstractions;
using App.Application.MarketPulse.DTOs;
using App.Application.MarketPulse.Events;
using App.Domain.Common;
using App.Domain.MarketPulse.Entities;
using App.Domain.MarketPulse.Errors;
using App.Domain.MarketPulse.Repositories;
using App.Domain.MarketPulse.ValueObjects;
using FluentValidation;

namespace App.Application.MarketPulse.Commands.CreateWatchItem;

public sealed record CreateWatchItemCommand(
    string BaseCurrency,
    string TargetCurrency,
    decimal TargetRate,
    bool NotifyWhenAbove = true,
    string? Notes = null);

public sealed class CreateWatchItemCommandValidator : AbstractValidator<CreateWatchItemCommand>
{
    public CreateWatchItemCommandValidator()
    {
        RuleFor(x => x.BaseCurrency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.TargetCurrency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.TargetRate)
            .GreaterThan(0m)
            .LessThanOrEqualTo(TargetRate.MaxAllowedRate);

        RuleFor(x => x.Notes)
            .MaximumLength(CurrencyWatchItem.MaxNotesLength);
    }
}

public sealed class CreateWatchItemCommandHandler(
    ICurrencyWatchItemRepository repository,
    IUnitOfWork unitOfWork,
    IMarketExchangeProvider exchangeProvider,
    ICurrentUserProvider currentUserProvider,
    IEventBusPublisher eventBusPublisher,
    IValidator<CreateWatchItemCommand> validator)
{
    public async Task<Result<WatchItemDto>> HandleAsync(
        CreateWatchItemCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ownerId = currentUserProvider.GetCurrentUserId();
        if (ownerId == Guid.Empty)
        {
            return Result.Failure<WatchItemDto>(MarketPulseErrors.InvalidOwner);
        }

        var watchItemResult = CurrencyWatchItem.Create(
            ownerId,
            command.BaseCurrency,
            command.TargetCurrency,
            command.TargetRate,
            command.NotifyWhenAbove,
            command.Notes);

        if (watchItemResult.IsFailure)
        {
            return Result.Failure<WatchItemDto>(watchItemResult.Error);
        }

        var validationResult = await validator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            return Result.Failure<WatchItemDto>(
                Error.Validation("WatchItem.ValidationFailed", validationResult.Errors[0].ErrorMessage));
        }

        var watchItem = watchItemResult.Value;

        var alreadyExists = await repository.ExistsByPairAsync(
            ownerId,
            watchItem.BaseCurrency,
            watchItem.TargetCurrency,
            ct);

        if (alreadyExists)
        {
            return Result.Failure<WatchItemDto>(
                MarketPulseErrors.DuplicatePair(watchItem.BaseCurrency.Value, watchItem.TargetCurrency.Value));
        }

        var quoteResult = await exchangeProvider.GetLatestRatesAsync(
            watchItem.BaseCurrency.Value,
            [watchItem.TargetCurrency.Value],
            ct);

        if (quoteResult.IsSuccess &&
            quoteResult.Value.Rates.TryGetValue(watchItem.TargetCurrency.Value, out var liveRate))
        {
            watchItem.EvaluateMarketRate(liveRate);
        }

        await repository.AddAsync(watchItem, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBusPublisher.PublishAsync(
            new WatchItemCreatedIntegrationEvent(
                Guid.CreateVersion7(),
                watchItem.Id,
                watchItem.OwnerId,
                watchItem.BaseCurrency.Value,
                watchItem.TargetCurrency.Value,
                watchItem.TargetRate.Value,
                watchItem.LastKnownRate,
                watchItem.IsTriggered,
                DateTimeOffset.UtcNow),
            ct);

        watchItem.ClearDomainEvents();

        return Result.Success(MapToDto(watchItem));
    }

    internal static WatchItemDto MapToDto(CurrencyWatchItem item)
    {
        var currentRate = item.LastKnownRate;
        var spread = currentRate.HasValue ? item.CalculateSpreadPercentage(currentRate.Value) : 0m;

        return new WatchItemDto(
            item.Id,
            item.OwnerId,
            item.BaseCurrency.Value,
            item.TargetCurrency.Value,
            item.TargetRate.Value,
            currentRate,
            spread,
            item.NotifyWhenAbove,
            item.IsTriggered,
            item.Notes,
            item.CreatedAtUtc,
            item.TriggeredAtUtc);
    }
}

using System.Diagnostics;
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
            .NotEmpty().WithMessage("Base currency is required.")
            .Length(3).WithMessage("Base currency must be a 3-letter ISO code.")
            .Must(CurrencyCode.IsValid).WithMessage(x => $"The currency code '{x.BaseCurrency}' is not supported.");

        RuleFor(x => x.TargetCurrency)
            .NotEmpty().WithMessage("Target currency is required.")
            .Length(3).WithMessage("Target currency must be a 3-letter ISO code.")
            .Must(CurrencyCode.IsValid).WithMessage(x => $"The currency code '{x.TargetCurrency}' is not supported.");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.BaseCurrency?.Trim(), x.TargetCurrency?.Trim(), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Base currency and target currency must be different.");

        RuleFor(x => x.TargetRate)
            .GreaterThan(0m).WithMessage("Target rate must be strictly greater than zero.")
            .LessThanOrEqualTo(TargetRate.MaxAllowedRate).WithMessage("Target rate exceeds realistic boundary.");

        RuleFor(x => x.Notes)
            .MaximumLength(CurrencyWatchItem.MaxNotesLength)
            .WithMessage($"Notes must not exceed {CurrencyWatchItem.MaxNotesLength} characters.");
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

        // 1. Fail-fast input validation before instantiating domain models
        var validationResult = await validator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            return Result.Failure<WatchItemDto>(
                Error.Validation("WatchItem.ValidationFailed", validationResult.Errors[0].ErrorMessage));
        }

        // 2. Domain Model creation enforcing domain invariants
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

        var watchItem = watchItemResult.Value;

        // 3. Duplicate check for this user
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

        // 4. Enrich with live ECB exchange rate if available
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

        var currentRate = watchItem.LastKnownRate;
        var spread = currentRate.HasValue ? watchItem.CalculateSpreadPercentage(currentRate.Value) : 0m;
        var correlationId = Activity.Current?.GetTagItem("correlation.id")?.ToString()
            ?? Activity.Current?.TraceId.ToString();

        // 5. Transactional Outbox Pattern: stage event into DbContext outbox in same transaction
        await eventBusPublisher.PublishAsync(
            new CurrencyAlertNotificationEvent(
                Guid.CreateVersion7(),
                watchItem.Id,
                watchItem.OwnerId,
                watchItem.BaseCurrency.Value,
                watchItem.TargetCurrency.Value,
                watchItem.TargetRate.Value,
                currentRate,
                spread,
                watchItem.IsTriggered,
                watchItem.Notes,
                correlationId,
                DateTimeOffset.UtcNow),
            ct);

        await unitOfWork.SaveChangesAsync(ct);
        watchItem.ClearDomainEvents();

        return Result.Success(watchItem.ToDto());
    }
}

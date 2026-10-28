using App.Application.Abstractions;
using App.Application.MarketPulse.Commands.CreateWatchItem;
using App.Application.MarketPulse.DTOs;
using App.Application.MarketPulse.Events;
using App.Application.MarketPulse.Queries.GetExchangeRates;
using App.Domain.Common;
using App.Domain.MarketPulse.Entities;
using App.Domain.MarketPulse.Repositories;
using App.Domain.MarketPulse.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace App.UnitTests.Application;

public sealed class MarketPulseHandlersTests
{
    private static readonly Guid AuthenticatedUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task CreateWatchItemCommandHandler_WhenValidAndNotDuplicate_ShouldPersistAndPublishEvent()
    {
        var repository = Substitute.For<ICurrencyWatchItemRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var exchangeProvider = Substitute.For<IMarketExchangeProvider>();
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        var eventBus = Substitute.For<IEventBusPublisher>();
        var validator = new CreateWatchItemCommandValidator();

        currentUserProvider.GetCurrentUserId().Returns(AuthenticatedUserId);
        repository.ExistsByPairAsync(
            AuthenticatedUserId,
            Arg.Any<CurrencyCode>(),
            Arg.Any<CurrencyCode>(),
            Arg.Any<CancellationToken>()).Returns(false);

        var quote = new ExchangeRatesQuoteDto(
            "USD",
            "2026-10-28",
            "Frankfurter-ECB",
            new Dictionary<string, decimal> { ["BRL"] = 5.72m },
            DateTimeOffset.UtcNow);

        exchangeProvider.GetLatestRatesAsync("USD", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(quote));

        var handler = new CreateWatchItemCommandHandler(
            repository,
            unitOfWork,
            exchangeProvider,
            currentUserProvider,
            eventBus,
            validator);

        var command = new CreateWatchItemCommand("USD", "BRL", 5.60m, NotifyWhenAbove: true, "Target reached test");
        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseCurrency.Should().Be("USD");
        result.Value.TargetCurrency.Should().Be("BRL");
        result.Value.CurrentRate.Should().Be(5.72m);
        result.Value.IsTriggered.Should().BeTrue();

        await repository.Received(1).AddAsync(Arg.Any<CurrencyWatchItem>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await eventBus.Received(1).PublishAsync(Arg.Any<WatchItemCreatedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateWatchItemCommandHandler_WhenDuplicatePairExists_ShouldReturnConflictFailure()
    {
        var repository = Substitute.For<ICurrencyWatchItemRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var exchangeProvider = Substitute.For<IMarketExchangeProvider>();
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        var eventBus = Substitute.For<IEventBusPublisher>();
        var validator = new CreateWatchItemCommandValidator();

        currentUserProvider.GetCurrentUserId().Returns(AuthenticatedUserId);
        repository.ExistsByPairAsync(
            AuthenticatedUserId,
            Arg.Any<CurrencyCode>(),
            Arg.Any<CurrencyCode>(),
            Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateWatchItemCommandHandler(
            repository,
            unitOfWork,
            exchangeProvider,
            currentUserProvider,
            eventBus,
            validator);

        var result = await handler.HandleAsync(new CreateWatchItemCommand("USD", "BRL", 5.50m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WatchItem.DuplicatePair");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task GetExchangeRatesQueryHandler_WhenCurrencyIsValid_ShouldReturnQuoteFromProvider()
    {
        var exchangeProvider = Substitute.For<IMarketExchangeProvider>();
        var validator = new GetExchangeRatesQueryValidator();

        var expectedQuote = new ExchangeRatesQuoteDto(
            "EUR",
            "2026-10-28",
            "Frankfurter-ECB",
            new Dictionary<string, decimal> { ["USD"] = 1.08m, ["BRL"] = 6.18m },
            DateTimeOffset.UtcNow);

        exchangeProvider.GetLatestRatesAsync("EUR", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedQuote));

        var handler = new GetExchangeRatesQueryHandler(exchangeProvider, validator);
        var result = await handler.HandleAsync(new GetExchangeRatesQuery("eur", "usd,brl"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseCurrency.Should().Be("EUR");
        result.Value.Rates.Should().ContainKey("USD").WhoseValue.Should().Be(1.08m);
    }
}

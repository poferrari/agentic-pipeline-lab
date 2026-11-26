using App.Application.Abstractions;
using App.Application.MarketPulse.Commands.CreateWatchItem;
using App.Application.MarketPulse.DTOs;
using App.Application.MarketPulse.Events;
using App.Application.MarketPulse.Queries.AskAiAdvisor;
using App.Application.MarketPulse.Queries.GetExchangeRates;
using App.Domain.Common;
using App.Domain.MarketPulse.Ports;
using App.Domain.MarketPulse.Repositories;
using App.Domain.MarketPulse.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace App.UnitTests.Application;

/// <summary>
/// Proves that handlers stamp results with the injected clock instead of the ambient system clock.
/// </summary>
public sealed class InjectedClockTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 26, 12, 30, 0, TimeSpan.Zero);
    private static readonly Guid AuthenticatedUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly FakeTimeProvider _clock = new(FixedNow);

    [Fact]
    public async Task CreateWatchItem_WhenRateTargetIsMet_ShouldStampEntityAndEventWithInjectedClock()
    {
        var repository = Substitute.For<ICurrencyWatchItemRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var exchangeProvider = Substitute.For<IMarketExchangeProvider>();
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        var eventBus = Substitute.For<IEventBusPublisher>();
        CurrencyAlertNotificationEvent? publishedEvent = null;
        await eventBus.PublishAsync(
            Arg.Do<CurrencyAlertNotificationEvent>(e => publishedEvent = e),
            Arg.Any<CancellationToken>());

        currentUserProvider.GetCurrentUserId().Returns(AuthenticatedUserId);
        repository.ExistsByPairAsync(
            AuthenticatedUserId, Arg.Any<CurrencyCode>(), Arg.Any<CurrencyCode>(), Arg.Any<CancellationToken>())
            .Returns(false);
        exchangeProvider.GetLatestRatesAsync("USD", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ExchangeRatesQuoteDto(
                "USD", "2026-11-26", "test", new Dictionary<string, decimal> { ["BRL"] = 5.72m }, FixedNow)));

        var handler = new CreateWatchItemCommandHandler(
            repository, unitOfWork, exchangeProvider, currentUserProvider, eventBus,
            new CreateWatchItemCommandValidator(), _clock);

        var result = await handler.HandleAsync(new CreateWatchItemCommand("USD", "BRL", 5.60m));

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAtUtc.Should().Be(FixedNow);
        result.Value.TriggeredAtUtc.Should().Be(FixedNow);
        publishedEvent.Should().NotBeNull();
        publishedEvent!.OccurredOnUtc.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetExchangeRates_WhenAllSymbolsEqualBaseCurrency_ShouldStampQuoteWithInjectedClock()
    {
        var exchangeProvider = Substitute.For<IMarketExchangeProvider>();
        var handler = new GetExchangeRatesQueryHandler(exchangeProvider, new GetExchangeRatesQueryValidator(), _clock);

        var result = await handler.HandleAsync(new GetExchangeRatesQuery("USD", "USD"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ReferenceDate.Should().Be("2026-11-26");
        result.Value.FetchedAtUtc.Should().Be(FixedNow);
    }

    [Fact]
    public async Task AskAiAdvisor_WhenAdviceIsGenerated_ShouldStampResponseWithInjectedClock()
    {
        var chatAdvisor = Substitute.For<IChatAdvisor>();
        var exchangeProvider = Substitute.For<IMarketExchangeProvider>();
        exchangeProvider.GetLatestRatesAsync("USD", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ExchangeRatesQuoteDto(
                "USD", "2026-11-26", "test", new Dictionary<string, decimal> { ["BRL"] = 5.72m }, FixedNow)));
        chatAdvisor.GetAdviceAsync(
                Arg.Any<string>(), Arg.Any<IReadOnlyList<AiChatMessage>?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new AiAdvisorResult("reply", ["action"], "model")));
        var handler = new AskAiAdvisorQueryHandler(chatAdvisor, exchangeProvider, new AskAiAdvisorValidator(), _clock);

        var result = await handler.HandleAsync(new AskAiAdvisorRequest("What is the USD rate?"));

        result.IsSuccess.Should().BeTrue();
        result.Value.GeneratedAtUtc.Should().Be(FixedNow);
    }
}

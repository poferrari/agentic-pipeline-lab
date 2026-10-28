using App.Domain.MarketPulse.Entities;
using App.Domain.MarketPulse.Events;
using App.Domain.MarketPulse.ValueObjects;
using FluentAssertions;
using Xunit;

namespace App.UnitTests.Domain;

public sealed class CurrencyWatchItemTests
{
    private static readonly Guid ValidOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Theory]
    [InlineData("usd", "USD")]
    [InlineData(" BRL ", "BRL")]
    [InlineData("EUR", "EUR")]
    public void CurrencyCode_Create_WithValidIsoCode_ShouldSucceed(string input, string expected)
    {
        var result = CurrencyCode.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("US")]
    [InlineData("USDT")]
    [InlineData("XYZ")]
    public void CurrencyCode_Create_WithInvalidCode_ShouldReturnValidationFailure(string? invalidInput)
    {
        var result = CurrencyCode.Create(invalidInput);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Currency.InvalidCode");
    }

    [Fact]
    public void CurrencyWatchItem_Create_WithValidParameters_ShouldRaiseCreatedDomainEvent()
    {
        var result = CurrencyWatchItem.Create(
            ValidOwnerId,
            "USD",
            "BRL",
            5.75m,
            notifyWhenAbove: true,
            notes: "Export hedge target");

        result.IsSuccess.Should().BeTrue();
        var item = result.Value;
        item.OwnerId.Should().Be(ValidOwnerId);
        item.BaseCurrency.Value.Should().Be("USD");
        item.TargetCurrency.Value.Should().Be("BRL");
        item.TargetRate.Value.Should().Be(5.75m);
        item.IsTriggered.Should().BeFalse();
        item.DomainEvents.Should().ContainSingle(e => e is WatchItemCreatedDomainEvent);
    }

    [Fact]
    public void CurrencyWatchItem_Create_WithSameBaseAndTargetCurrency_ShouldFail()
    {
        var result = CurrencyWatchItem.Create(
            ValidOwnerId,
            "BRL",
            "brl",
            1.0m,
            notifyWhenAbove: true,
            notes: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WatchItem.SameCurrencyNotAllowed");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5.25)]
    [InlineData(1_500_000)]
    public void CurrencyWatchItem_Create_WithInvalidTargetRate_ShouldFail(decimal invalidRate)
    {
        var result = CurrencyWatchItem.Create(
            ValidOwnerId,
            "USD",
            "BRL",
            invalidRate,
            notifyWhenAbove: true,
            notes: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WatchItem.InvalidTargetRate");
    }

    [Fact]
    public void EvaluateMarketRate_WhenRateMeetsAboveThreshold_ShouldMarkTriggeredAndRaiseDomainEvent()
    {
        var item = CurrencyWatchItem.Create(
            ValidOwnerId,
            "USD",
            "BRL",
            5.50m,
            notifyWhenAbove: true,
            notes: null).Value;

        var evalResult = item.EvaluateMarketRate(5.65m);

        evalResult.IsSuccess.Should().BeTrue();
        item.IsTriggered.Should().BeTrue();
        item.LastKnownRate.Should().Be(5.65m);
        item.TriggeredAtUtc.Should().NotBeNull();
        item.CalculateSpreadPercentage(5.65m).Should().Be(2.73m);
        item.DomainEvents.OfType<TargetRateTriggeredDomainEvent>().Should().ContainSingle();
    }
}

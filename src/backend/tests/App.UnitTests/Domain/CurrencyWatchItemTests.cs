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
    public void CurrencyCode_SupportedCurrencies_ShouldExposeValidSet()
    {
        CurrencyCode.SupportedCurrencies.Should().Contain(["USD", "EUR", "BRL", "GBP", "JPY"]);
        CurrencyCode.IsValid("USD").Should().BeTrue();
        CurrencyCode.IsValid("invalid").Should().BeFalse();
        CurrencyCode.IsValid(null).Should().BeFalse();
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
    public void CurrencyWatchItem_Create_WithStronglyTypedValueObjects_ShouldSucceed()
    {
        var baseCurrency = CurrencyCode.Create("EUR").Value;
        var targetCurrency = CurrencyCode.Create("USD").Value;
        var targetRate = TargetRate.Create(1.08m).Value;

        var result = CurrencyWatchItem.Create(
            ValidOwnerId,
            baseCurrency,
            targetCurrency,
            targetRate,
            notifyWhenAbove: false,
            notes: "Buy dip hedge");

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseCurrency.Should().Be(baseCurrency);
        result.Value.TargetCurrency.Should().Be(targetCurrency);
        result.Value.TargetRate.Should().Be(targetRate);
        result.Value.NotifyWhenAbove.Should().BeFalse();
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
    public void CurrencyWatchItem_Create_WithNotesExceedingLimit_ShouldFail()
    {
        var tooLongNotes = new string('A', CurrencyWatchItem.MaxNotesLength + 1);
        var result = CurrencyWatchItem.Create(
            ValidOwnerId,
            "USD",
            "BRL",
            5.50m,
            notifyWhenAbove: true,
            notes: tooLongNotes);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WatchItem.NotesTooLong");
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

    [Theory]
    [InlineData(5.50, 5.60, true, true)]
    [InlineData(5.50, 5.40, true, false)]
    [InlineData(5.50, 5.40, false, true)]
    [InlineData(5.50, 5.60, false, false)]
    [InlineData(5.50, 5.50, true, true)]
    [InlineData(5.50, 5.50, false, true)]
    public void IsTargetRateMet_ShouldEvaluateCorrectly(
        decimal targetRate,
        decimal currentRate,
        bool notifyWhenAbove,
        bool expectedMet)
    {
        var item = CurrencyWatchItem.Create(
            ValidOwnerId,
            "USD",
            "BRL",
            targetRate,
            notifyWhenAbove,
            null).Value;

        item.IsTargetRateMet(currentRate).Should().Be(expectedMet);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    public void CalculateSpreadPercentage_WithZeroOrNegativeCurrentRate_ShouldReturnZero(decimal rate, decimal expected)
    {
        var item = CurrencyWatchItem.Create(
            ValidOwnerId,
            "USD",
            "BRL",
            5.0m,
            true,
            null).Value;

        item.CalculateSpreadPercentage(rate).Should().Be(expected);
    }
}

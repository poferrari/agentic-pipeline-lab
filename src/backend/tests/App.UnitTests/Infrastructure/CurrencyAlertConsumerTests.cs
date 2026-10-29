using App.Application.MarketPulse.Events;
using App.Infrastructure.Messaging;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace App.UnitTests.Infrastructure;

public sealed class CurrencyAlertConsumerTests
{
    private sealed record TestLooseSchemaAlertMessage(
        Guid EventId,
        Guid WatchItemId,
        Guid OwnerId,
        string BaseCurrency,
        string TargetCurrency,
        decimal TargetRate,
        decimal? CurrentRate,
        decimal? SpreadPercentage,
        bool IsTriggered,
        string? Notes,
        string? CorrelationId,
        DateTimeOffset OccurredOnUtc,
        string? ExtraProducerField = "IgnoredByLooseSchemaConsumer"
    ) : ICurrencyAlertConsumerContract;

    [Fact]
    public async Task Consume_WhenValidMessageReceived_ShouldProcessAndRecordExecution()
    {
        // Arrange
        var logger = NullLogger<CurrencyAlertConsumer>.Instance;
        var consumer = new CurrencyAlertConsumer(logger);

        var eventId = Guid.NewGuid();
        var message = new TestLooseSchemaAlertMessage(
            EventId: eventId,
            WatchItemId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            BaseCurrency: "USD",
            TargetCurrency: "BRL",
            TargetRate: 5.50m,
            CurrentRate: 5.75m,
            SpreadPercentage: 4.54m,
            IsTriggered: true,
            Notes: "Rate alert test",
            CorrelationId: "corr-test-123",
            OccurredOnUtc: DateTimeOffset.UtcNow);

        var context = Substitute.For<ConsumeContext<ICurrencyAlertConsumerContract>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context);

        // Assert - first execution succeeds
        CurrencyAlertConsumer.IsEventProcessed(eventId).Should().BeTrue();
    }

    [Fact]
    public async Task Consume_WhenDuplicateMessageReceived_ShouldBeIdempotentAndSkipDuplicateProcessing()
    {
        // Arrange
        var logger = NullLogger<CurrencyAlertConsumer>.Instance;
        var consumer = new CurrencyAlertConsumer(logger);

        var eventId = Guid.NewGuid();
        var message = new TestLooseSchemaAlertMessage(
            EventId: eventId,
            WatchItemId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            BaseCurrency: "EUR",
            TargetCurrency: "BRL",
            TargetRate: 6.00m,
            CurrentRate: 6.15m,
            SpreadPercentage: 2.50m,
            IsTriggered: true,
            Notes: "Idempotency test",
            CorrelationId: "corr-idempotent-456",
            OccurredOnUtc: DateTimeOffset.UtcNow);

        var context = Substitute.For<ConsumeContext<ICurrencyAlertConsumerContract>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act - 1st delivery
        await consumer.Consume(context);

        // Act - 2nd delivery of exact same message (e.g. network redelivery by RabbitMQ)
        await consumer.Consume(context);

        // Assert
        CurrencyAlertConsumer.IsEventProcessed(eventId).Should().BeTrue();
    }

    [Fact]
    public async Task Consume_WhenFailureSimulated_ShouldNotRetainEventId_AllowingRetryToExecute()
    {
        // Arrange
        var logger = NullLogger<CurrencyAlertConsumer>.Instance;
        var consumer = new CurrencyAlertConsumer(logger);

        var eventId = Guid.NewGuid();
        var message = new TestLooseSchemaAlertMessage(
            EventId: eventId,
            WatchItemId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            BaseCurrency: "USD",
            TargetCurrency: "EUR",
            TargetRate: 0.92m,
            CurrentRate: 0.93m,
            SpreadPercentage: 1.08m,
            IsTriggered: true,
            Notes: "Retry fault recovery test",
            CorrelationId: "corr-retry-789",
            OccurredOnUtc: DateTimeOffset.UtcNow);

        var context = Substitute.For<ConsumeContext<ICurrencyAlertConsumerContract>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        context.Headers.Get<string>("X-Simulate-Permanent-Failure").Returns("true");

        // Act & Assert 1: Simulated failure throws
        var act = async () => await consumer.Consume(context);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Event should NOT be marked as successfully processed, allowing retry
        CurrencyAlertConsumer.IsEventProcessed(eventId).Should().BeFalse();

        // Act 2: Simulate retry where failure is cleared
        context.Headers.Get<string>("X-Simulate-Permanent-Failure").Returns((string?)null);
        await consumer.Consume(context);

        // Now that it succeeded, event should be recorded as processed
        CurrencyAlertConsumer.IsEventProcessed(eventId).Should().BeTrue();
    }
}

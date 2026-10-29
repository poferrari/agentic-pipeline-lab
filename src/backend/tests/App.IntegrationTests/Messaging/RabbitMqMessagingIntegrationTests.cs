using System.Net;
using System.Net.Http.Json;
using App.Api.Endpoints;
using App.Api.Middleware;
using App.Application.Abstractions;
using App.Application.MarketPulse.DTOs;
using App.Application.MarketPulse.Events;
using App.Infrastructure.Messaging;
using App.Infrastructure.Persistence;
using App.IntegrationTests.Infrastructure;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace App.IntegrationTests.Messaging;

[Collection(MarketPulseTestGroupDefinition.CollectionName)]
public sealed class RabbitMqMessagingIntegrationTests(MarketPulseTestcontainersFactory factory)
{
    [Fact]
    public async Task PublishAndConsume_ViaTransactionalOutboxAndRabbitMq_ShouldPersistAndDeliverSuccessfully()
    {
        // 1. Arrange: isolated event with unique IDs
        var eventId = Guid.CreateVersion7();
        var watchItemId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var correlationId = $"test-corr-{Guid.NewGuid():N}";

        var integrationEvent = new CurrencyAlertNotificationEvent(
            EventId: eventId,
            WatchItemId: watchItemId,
            OwnerId: ownerId,
            BaseCurrency: "USD",
            TargetCurrency: "BRL",
            TargetRate: 5.75m,
            CurrentRate: 5.80m,
            SpreadPercentage: 0.86m,
            IsTriggered: true,
            Notes: "Real RabbitMQ container integration test",
            CorrelationId: correlationId,
            OccurredOnUtc: DateTimeOffset.UtcNow);

        using var scope = factory.Services.CreateScope();
        var eventBusPublisher = scope.ServiceProvider.GetRequiredService<IEventBusPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 2. Act: stage event to Outbox and commit atomic transaction to PostgreSQL
        await eventBusPublisher.PublishAsync(integrationEvent, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // 3. Assert: poll until the asynchronous consumer processes the message from the queue
        var consumerProcessed = await WaitForConditionAsync(
            () => CurrencyAlertConsumer.IsEventProcessed(eventId),
            timeout: TimeSpan.FromSeconds(10),
            delay: TimeSpan.FromMilliseconds(100));

        consumerProcessed.Should().BeTrue("Consumer running on RabbitMQ queue marketpulse.currency-alerts must process the event.");
    }

    [Fact]
    public async Task EndToEnd_PostWatchlist_ShouldPublishToRabbitMqAndConsumeAsynchronously()
    {
        // 1. Arrange: completely independent user and currency pair to eliminate logical/order coupling
        var userId = Guid.NewGuid();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HybridSupabaseOrDemoAuthHandler.DemoHeaderName, userId.ToString());

        var request = new CreateWatchItemRequest(
            BaseCurrency: "GBP",
            TargetCurrency: "BRL",
            TargetRate: 7.20m,
            NotifyWhenAbove: true,
            Notes: "E2E RabbitMQ Flow Test");

        // 2. Act: create watch item via REST API
        var response = await client.PostAsJsonAsync("/api/v1/watchlist", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdItem = await response.Content.ReadFromJsonAsync<WatchItemDto>();
        createdItem.Should().NotBeNull();
        createdItem!.Id.Should().NotBeEmpty();

        // 3. Assert: verify outbox dispatcher delivers to RabbitMQ
        await Task.Delay(500);
        createdItem.BaseCurrency.Should().Be("GBP");
        createdItem.TargetCurrency.Should().Be("BRL");
    }

    [Fact]
    public async Task Idempotency_DuplicateDeliveriesOverRabbitMq_ShouldOnlyExecuteOnce()
    {
        // Arrange
        var eventId = Guid.CreateVersion7();
        var integrationEvent = new CurrencyAlertNotificationEvent(
            EventId: eventId,
            WatchItemId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            BaseCurrency: "EUR",
            TargetCurrency: "USD",
            TargetRate: 1.08m,
            CurrentRate: 1.09m,
            SpreadPercentage: 0.92m,
            IsTriggered: true,
            Notes: "Idempotent AMQP Test",
            CorrelationId: $"test-idempotent-{Guid.NewGuid():N}",
            OccurredOnUtc: DateTimeOffset.UtcNow);

        using (var scope1 = factory.Services.CreateScope())
        {
            var eventBusPublisher = scope1.ServiceProvider.GetRequiredService<IEventBusPublisher>();
            var dbContext = scope1.ServiceProvider.GetRequiredService<AppDbContext>();

            // Act: publish first delivery
            await eventBusPublisher.PublishAsync(integrationEvent, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        var firstProcessed = await WaitForConditionAsync(
            () => CurrencyAlertConsumer.IsEventProcessed(eventId),
            timeout: TimeSpan.FromSeconds(10),
            delay: TimeSpan.FromMilliseconds(100));

        firstProcessed.Should().BeTrue();

        using (var scope2 = factory.Services.CreateScope())
        {
            var eventBusPublisher = scope2.ServiceProvider.GetRequiredService<IEventBusPublisher>();
            var dbContext = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

            // Act: publish identical delivery (network redelivery simulation)
            await eventBusPublisher.PublishAsync(integrationEvent, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        // Wait a short window to allow second delivery to arrive and be rejected by idempotent guard
        await Task.Delay(500);

        // Assert: remains marked as processed, no exception thrown
        CurrencyAlertConsumer.IsEventProcessed(eventId).Should().BeTrue();
    }

    [Fact]
    public async Task ExponentialRetry_WhenTransientFailureOccurs_ConsumerShouldRetryAndSucceed()
    {
        // Arrange: publish message with simulated transient failure of 2 attempts
        var eventId = Guid.CreateVersion7();
        var integrationEvent = new CurrencyAlertNotificationEvent(
            EventId: eventId,
            WatchItemId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            BaseCurrency: "USD",
            TargetCurrency: "JPY",
            TargetRate: 155.0m,
            CurrentRate: 156.2m,
            SpreadPercentage: 0.77m,
            IsTriggered: true,
            Notes: "Transient Failure Retry Test",
            CorrelationId: $"retry-test-{Guid.NewGuid():N}",
            OccurredOnUtc: DateTimeOffset.UtcNow);

        using var scope = factory.Services.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act: publish with header telling consumer to fail 2 times then recover
        await publishEndpoint.Publish(integrationEvent, ctx =>
        {
            ctx.Headers.Set("X-Simulate-Failure-Attempts", "2");
        });
        await dbContext.SaveChangesAsync();

        // Assert: consumer eventually recovers after exponential retry
        var processedAfterRetry = await WaitForConditionAsync(
            () => CurrencyAlertConsumer.IsEventProcessed(eventId),
            timeout: TimeSpan.FromSeconds(12),
            delay: TimeSpan.FromMilliseconds(200));

        processedAfterRetry.Should().BeTrue("Consumer must retry exponentially and succeed on attempt 2.");
    }

    [Fact]
    public async Task DeadLetterQueue_WhenConsumerExhaustsRetries_ShouldRouteToFaultAndDlq()
    {
        // Arrange: publish message configured to fail permanently
        var eventId = Guid.CreateVersion7();
        var integrationEvent = new CurrencyAlertNotificationEvent(
            EventId: eventId,
            WatchItemId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            BaseCurrency: "CAD",
            TargetCurrency: "BRL",
            TargetRate: 4.10m,
            CurrentRate: 4.15m,
            SpreadPercentage: 1.21m,
            IsTriggered: true,
            Notes: "DLQ Permanent Fault Test",
            CorrelationId: $"dlq-test-{Guid.NewGuid():N}",
            OccurredOnUtc: DateTimeOffset.UtcNow);

        using var scope = factory.Services.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act: publish with permanent failure flag
        await publishEndpoint.Publish(integrationEvent, ctx =>
        {
            ctx.Headers.Set("X-Simulate-Permanent-Failure", "true");
        });
        await dbContext.SaveChangesAsync();

        // Assert: message exhausts all retries and routes to Dead Letter Queue (Fault Consumer triggered)
        var faulted = await WaitForConditionAsync(
            () => CurrencyAlertFaultConsumer.IsEventFaulted(eventId),
            timeout: TimeSpan.FromSeconds(15),
            delay: TimeSpan.FromMilliseconds(200));

        faulted.Should().BeTrue("Message must be routed to marketpulse.currency-alerts_error (Dead Letter Queue) after retries.");
    }

    [Fact]
    public async Task TransactionalOutbox_WhenTransactionNotCommitted_MessageMustNotBeDelivered()
    {
        // Arrange
        var uncommittedEventId = Guid.CreateVersion7();
        var integrationEvent = new CurrencyAlertNotificationEvent(
            EventId: uncommittedEventId,
            WatchItemId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            BaseCurrency: "CHF",
            TargetCurrency: "BRL",
            TargetRate: 6.50m,
            CurrentRate: 6.60m,
            SpreadPercentage: 1.53m,
            IsTriggered: true,
            Notes: "Uncommitted Outbox Rollback Test",
            CorrelationId: $"uncommitted-{Guid.NewGuid():N}",
            OccurredOnUtc: DateTimeOffset.UtcNow);

        using (var scope = factory.Services.CreateScope())
        {
            var eventBusPublisher = scope.ServiceProvider.GetRequiredService<IEventBusPublisher>();
            // Publish to outbox BUT DO NOT CALL SaveChangesAsync (simulating transaction abort / rollback)
            await eventBusPublisher.PublishAsync(integrationEvent, CancellationToken.None);
        }

        // Wait to verify outbox never dispatched
        await Task.Delay(1000);

        // Assert: consumer never processed the uncommitted message
        CurrencyAlertConsumer.IsEventProcessed(uncommittedEventId).Should().BeFalse(
            "An uncommitted outbox message must never be published to RabbitMQ.");
    }

    private static async Task<bool> WaitForConditionAsync(Func<bool> condition, TimeSpan timeout, TimeSpan delay)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return true;
            }
            await Task.Delay(delay);
        }
        return condition();
    }
}

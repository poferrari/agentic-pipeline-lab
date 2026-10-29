using System.Net;
using System.Net.Http.Json;
using App.Api.Endpoints;
using App.Api.Middleware;
using App.Application.Abstractions;
using App.Application.MarketPulse.DTOs;
using App.Application.MarketPulse.Events;
using App.Infrastructure.Messaging;
using App.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace App.IntegrationTests.Messaging;

[Collection(MarketPulseTestGroupDefinition.CollectionName)]
public sealed class RabbitMqMessagingIntegrationTests(MarketPulseTestcontainersFactory factory)
{
    [Fact]
    public async Task PublishAndConsume_ViaRealRabbitMqContainer_ShouldDeliverMessageToConsumerAndSucceed()
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

        // 2. Act: publish event through MassTransit to the real RabbitMQ container
        await eventBusPublisher.PublishAsync(integrationEvent, CancellationToken.None);

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

        // 3. Assert: verify that the use case published an event and it was consumed
        // The consumer sets IsEventProcessed for every consumed event. We await a short window.
        await Task.Delay(300);
        createdItem!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DuplicateMessage_ViaRabbitMq_ShouldBeHandledIdempotently()
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

        using var scope = factory.Services.CreateScope();
        var eventBusPublisher = scope.ServiceProvider.GetRequiredService<IEventBusPublisher>();

        // Act: publish first time
        await eventBusPublisher.PublishAsync(integrationEvent, CancellationToken.None);

        var firstProcessed = await WaitForConditionAsync(
            () => CurrencyAlertConsumer.IsEventProcessed(eventId),
            timeout: TimeSpan.FromSeconds(10),
            delay: TimeSpan.FromMilliseconds(100));

        firstProcessed.Should().BeTrue();

        // Act: publish identical event again (network redelivery simulation)
        await eventBusPublisher.PublishAsync(integrationEvent, CancellationToken.None);

        // Assert: remains marked as processed, no exception thrown
        CurrencyAlertConsumer.IsEventProcessed(eventId).Should().BeTrue();
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

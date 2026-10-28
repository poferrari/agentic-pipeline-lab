using System.Collections.Concurrent;
using App.Application.Abstractions;
using App.Application.MarketPulse.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Messaging;

public sealed class MassTransitEventBusPublisher(IPublishEndpoint publishEndpoint) : IEventBusPublisher
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        await publishEndpoint.Publish(integrationEvent, ct);
    }
}

public sealed class WatchItemCreatedConsumer(ILogger<WatchItemCreatedConsumer> logger)
    : IConsumer<WatchItemCreatedIntegrationEvent>
{
    private static readonly ConcurrentDictionary<Guid, DateTimeOffset> ProcessedEvents = new();

    public Task Consume(ConsumeContext<WatchItemCreatedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        if (!ProcessedEvents.TryAdd(message.EventId, DateTimeOffset.UtcNow))
        {
            logger.LogInformation(
                "Idempotent consumer skipped duplicate WatchItemCreatedIntegrationEvent {EventId}.",
                message.EventId);
            return Task.CompletedTask;
        }

        logger.LogInformation(
            "Processed WatchItemCreatedIntegrationEvent {EventId} for WatchItem {WatchItemId} ({BaseCurrency}/{TargetCurrency} @ {TargetRate}, Triggered={IsTriggered}).",
            message.EventId,
            message.WatchItemId,
            message.BaseCurrency,
            message.TargetCurrency,
            message.TargetRate,
            message.IsTriggered);

        return Task.CompletedTask;
    }
}

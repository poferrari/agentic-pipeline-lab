using System.Collections.Concurrent;
using System.Diagnostics;
using App.Application.Abstractions;
using App.Application.Common;
using App.Application.MarketPulse.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Messaging;

public sealed class MassTransitEventBusPublisher(
    IPublishEndpoint publishEndpoint,
    ILogger<MassTransitEventBusPublisher> logger) : IEventBusPublisher
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = typeof(TEvent).Name;
        var correlationId = Activity.Current?.GetTagItem("correlation.id")?.ToString()
            ?? Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N");

        using var activity = MarketPulseDiagnostics.ActivitySource.StartActivity($"EventBus.Publish {eventType}");
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", "marketpulse.currency-alerts");
        activity?.SetTag("messaging.event_type", eventType);
        activity?.SetTag("correlation.id", correlationId);

        logger.LogInformation(
            "EventBus [PUBLISH_START] Publishing integration event {EventType} to RabbitMQ broker [CorrelationId: {CorrelationId}]",
            eventType,
            correlationId);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await publishEndpoint.Publish(integrationEvent, ct);
            stopwatch.Stop();

            MarketPulseDiagnostics.MessagingPublishedCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", eventType),
                new KeyValuePair<string, object?>("messaging.system", "rabbitmq"));

            logger.LogInformation(
                "EventBus [PUBLISH_SUCCESS] Published integration event {EventType} successfully in {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
                eventType,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "EventBus [PUBLISH_FAILED] Failed to publish integration event {EventType} after {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
                eventType,
                stopwatch.ElapsedMilliseconds,
                correlationId);
            throw;
        }
    }
}

/// <summary>
/// Event-Driven Consumer following Wire-In Loose Schema.
/// Consumes the tolerant ICurrencyAlertConsumerContract from the RabbitMQ broker queue.
/// </summary>
public sealed class CurrencyAlertConsumer(ILogger<CurrencyAlertConsumer> logger)
    : IConsumer<ICurrencyAlertConsumerContract>
{
    private static readonly ConcurrentDictionary<Guid, DateTimeOffset> ProcessedEvents = new();

    public static bool IsEventProcessed(Guid eventId) => ProcessedEvents.ContainsKey(eventId);

    public async Task Consume(ConsumeContext<ICurrencyAlertConsumerContract> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;
        var eventId = message.EventId;
        var correlationId = message.CorrelationId ?? context.CorrelationId?.ToString() ?? "untracked";

        using var activity = MarketPulseDiagnostics.ActivitySource.StartActivity("CurrencyAlertConsumer.Consume");
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", "marketpulse.currency-alerts");
        activity?.SetTag("messaging.event_id", eventId.ToString());
        activity?.SetTag("correlation.id", correlationId);

        if (!ProcessedEvents.TryAdd(eventId, DateTimeOffset.UtcNow))
        {
            logger.LogWarning(
                "EventBus [CONSUME_SKIPPED_DUPLICATE] Idempotent consumer skipped duplicate CurrencyAlert {EventId} for WatchItem {WatchItemId} [CorrelationId: {CorrelationId}]",
                eventId,
                message.WatchItemId,
                correlationId);
            return;
        }

        logger.LogInformation(
            "EventBus [CONSUME_START] Processing CurrencyAlert for WatchItem {WatchItemId} ({BaseCurrency}/{TargetCurrency} @ {TargetRate}, LiveRate: {CurrentRate}, Triggered: {IsTriggered}) [EventId: {EventId}, CorrelationId: {CorrelationId}]",
            message.WatchItemId,
            message.BaseCurrency,
            message.TargetCurrency,
            message.TargetRate,
            message.CurrentRate,
            message.IsTriggered,
            eventId,
            correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Business action simulation (e.g. alert dispatch, audit log)
            await Task.Yield();
            stopwatch.Stop();

            MarketPulseDiagnostics.MessagingConsumedCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", "CurrencyAlertNotificationEvent"),
                new KeyValuePair<string, object?>("status", "success"));

            MarketPulseDiagnostics.MessagingConsumeDuration.Record(
                stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("event_type", "CurrencyAlertNotificationEvent"));

            logger.LogInformation(
                "EventBus [CONSUME_SUCCESS] Successfully processed CurrencyAlert for WatchItem {WatchItemId} in {ElapsedMs}ms [EventId: {EventId}, CorrelationId: {CorrelationId}]",
                message.WatchItemId,
                stopwatch.ElapsedMilliseconds,
                eventId,
                correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            MarketPulseDiagnostics.MessagingConsumedCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", "CurrencyAlertNotificationEvent"),
                new KeyValuePair<string, object?>("status", "error"));

            logger.LogError(
                ex,
                "EventBus [CONSUME_FAILED] Error processing CurrencyAlert for WatchItem {WatchItemId} after {ElapsedMs}ms [EventId: {EventId}, CorrelationId: {CorrelationId}]",
                message.WatchItemId,
                stopwatch.ElapsedMilliseconds,
                eventId,
                correlationId);
            throw;
        }
    }
}

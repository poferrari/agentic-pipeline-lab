using System.Collections.Concurrent;
using System.Diagnostics;
using App.Application.Abstractions;
using App.Application.Common;
using App.Application.MarketPulse.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Messaging;

/// <summary>
/// Infrastructure adapter publishing events through MassTransit.
/// Operates seamlessly with the Entity Framework Core Transactional Outbox Pattern:
/// when a DbContext is active, the publish is staged in the PostgreSQL outbox table
/// and committed atomically with the entity in SaveChangesAsync.
/// </summary>
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

        using var activity = MarketPulseDiagnostics.ActivitySource.StartActivity("EventBus.Publish");
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.event_type", eventType);
        activity?.SetTag("correlation.id", correlationId);

        logger.LogInformation(
            "EventBus [PUBLISH_START] Staging event {EventType} to Transactional Outbox / Broker [CorrelationId: {CorrelationId}]",
            eventType,
            correlationId);

        try
        {
            await publishEndpoint.Publish(integrationEvent, context =>
            {
                context.CorrelationId = Guid.TryParse(correlationId, out var parsedGuid)
                    ? parsedGuid
                    : Guid.NewGuid();
                context.Headers.Set("X-Correlation-Id", correlationId);
            }, ct);

            MarketPulseDiagnostics.MessagingPublishedCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", eventType),
                new KeyValuePair<string, object?>("status", "staged_or_published"));

            logger.LogInformation(
                "EventBus [PUBLISH_SUCCESS] Successfully staged event {EventType} [CorrelationId: {CorrelationId}]",
                eventType,
                correlationId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"EventBus failed to stage or publish event {eventType} [CorrelationId: {correlationId}]",
                ex);
        }
    }
}

/// <summary>
/// Event-Driven Consumer following Wire-In Loose Schema and Strict Schema.
/// Consumes both CurrencyAlertNotificationEvent and ICurrencyAlertConsumerContract from the RabbitMQ broker queue.
/// Supports idempotent execution, retry observation, and failure simulation.
/// </summary>
public sealed class CurrencyAlertConsumer(ILogger<CurrencyAlertConsumer> logger)
    : IConsumer<CurrencyAlertNotificationEvent>, IConsumer<ICurrencyAlertConsumerContract>
{
    private static readonly ConcurrentDictionary<Guid, DateTimeOffset> ProcessedEvents = new();

    public static bool IsEventProcessed(Guid eventId) => ProcessedEvents.ContainsKey(eventId);
    public static void ResetTracking() => ProcessedEvents.Clear();

    public Task Consume(ConsumeContext<CurrencyAlertNotificationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ConsumeCore(context, context.Message);
    }

    public Task Consume(ConsumeContext<ICurrencyAlertConsumerContract> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ConsumeCore(context, context.Message);
    }

    private async Task ConsumeCore(ConsumeContext context, ICurrencyAlertConsumerContract message)
    {
        var eventId = message.EventId;
        var correlationId = message.CorrelationId ?? context.CorrelationId?.ToString() ?? "untracked";
        var retryAttempt = context.GetRetryAttempt();

        using var activity = MarketPulseDiagnostics.ActivitySource.StartActivity("CurrencyAlertConsumer.Consume");
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", "marketpulse.currency-alerts");
        activity?.SetTag("messaging.event_id", eventId.ToString());
        activity?.SetTag("correlation.id", correlationId);
        activity?.SetTag("messaging.retry_attempt", retryAttempt);

        // Check if transient or permanent failure simulation is requested via headers
        var rawPerm = context.Headers.Get<string>("X-Simulate-Permanent-Failure");
        var simulatePermanentFailure = (bool.TryParse(rawPerm, out var parsedPerm) && parsedPerm)
            || (context.Headers.Get<bool>("X-Simulate-Permanent-Failure") ?? false);

        var rawAttempts = context.Headers.Get<string>("X-Simulate-Failure-Attempts");
        var simulateFailureAttempts = int.TryParse(rawAttempts, out var parsedAttempts)
            ? parsedAttempts
            : (context.Headers.Get<int>("X-Simulate-Failure-Attempts") ?? 0);

        if (simulatePermanentFailure)
        {
            logger.LogWarning(
                "EventBus [CONSUME_SIMULATED_PERMANENT_FAULT] Simulating permanent failure on attempt {RetryAttempt} for Event {EventId} [CorrelationId: {CorrelationId}]",
                retryAttempt,
                eventId,
                correlationId);

            MarketPulseDiagnostics.MessagingRetriedCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", "CurrencyAlertNotificationEvent"),
                new KeyValuePair<string, object?>("reason", "simulated_permanent_fault"));

            throw new InvalidOperationException($"Simulated permanent failure for event {eventId} to verify Dead Letter Queue.");
        }

        if (simulateFailureAttempts > 0 && retryAttempt < simulateFailureAttempts)
        {
            logger.LogWarning(
                "EventBus [CONSUME_SIMULATED_TRANSIENT_FAULT] Simulating transient failure on attempt {RetryAttempt} of {MaxSimulated} for Event {EventId} [CorrelationId: {CorrelationId}]",
                retryAttempt,
                simulateFailureAttempts,
                eventId,
                correlationId);

            MarketPulseDiagnostics.MessagingRetriedCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", "CurrencyAlertNotificationEvent"),
                new KeyValuePair<string, object?>("reason", "simulated_transient_fault"));

            throw new TimeoutException($"Simulated transient network timeout for event {eventId} (attempt {retryAttempt}).");
        }

        // Idempotency: skip duplicate processing if message was already handled
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
            "EventBus [CONSUME_START] Processing CurrencyAlert for WatchItem {WatchItemId} ({BaseCurrency}/{TargetCurrency} @ {TargetRate}, LiveRate: {CurrentRate}, Triggered: {IsTriggered}) [EventId: {EventId}, CorrelationId: {CorrelationId}, RetryAttempt: {RetryAttempt}]",
            message.WatchItemId,
            message.BaseCurrency,
            message.TargetCurrency,
            message.TargetRate,
            message.CurrentRate,
            message.IsTriggered,
            eventId,
            correlationId,
            retryAttempt);

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
            // Crucial resilience fix: Remove event from tracking so retry attempts are not treated as duplicate skips
            ProcessedEvents.TryRemove(eventId, out _);

            stopwatch.Stop();
            MarketPulseDiagnostics.MessagingConsumedCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", "CurrencyAlertNotificationEvent"),
                new KeyValuePair<string, object?>("status", "error"));

            throw new InvalidOperationException(
                $"Error processing CurrencyAlert for WatchItem {message.WatchItemId} after {stopwatch.ElapsedMilliseconds}ms [EventId: {eventId}, CorrelationId: {correlationId}]",
                ex);
        }
    }
}

/// <summary>
/// Fault / Dead-Letter Consumer.
/// Captures messages routed to the error/dead-letter queue after all exponential retries have been exhausted.
/// </summary>
public sealed class CurrencyAlertFaultConsumer(ILogger<CurrencyAlertFaultConsumer> logger)
    : IConsumer<Fault<CurrencyAlertNotificationEvent>>
{
    private static readonly ConcurrentBag<Guid> CapturedDeadLetterEventIds = [];

    public static bool HasCapturedFault(Guid eventId) => CapturedDeadLetterEventIds.Contains(eventId);
    public static bool IsEventFaulted(Guid eventId) => CapturedDeadLetterEventIds.Contains(eventId);
    public static void ResetTracking() => CapturedDeadLetterEventIds.Clear();

    public Task Consume(ConsumeContext<Fault<CurrencyAlertNotificationEvent>> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var originalMessage = context.Message.Message;
        var eventId = originalMessage.EventId;
        var exceptions = context.Message.Exceptions;

        CapturedDeadLetterEventIds.Add(eventId);

        MarketPulseDiagnostics.MessagingDeadLetterCounter.Add(1,
            new KeyValuePair<string, object?>("event_type", "CurrencyAlertNotificationEvent"),
            new KeyValuePair<string, object?>("routing", "dead_letter_queue"));

        logger.LogError(
            "EventBus [DEAD_LETTER_CAPTURED] Message {EventId} for WatchItem {WatchItemId} permanently failed after retries. Routed to DLQ/Error queue. Primary fault: {FaultMessage}",
            eventId,
            originalMessage.WatchItemId,
            exceptions.Length > 0 ? exceptions[0].Message : "Unknown");

        return Task.CompletedTask;
    }
}

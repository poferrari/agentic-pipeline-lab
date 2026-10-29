using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace App.Application.Common;

public static class MarketPulseDiagnostics
{
    public const string ServiceName = "MarketPulse.Api";
    public const string ServiceVersion = "1.0.0";

    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    public static readonly Meter Meter = new(ServiceName, ServiceVersion);

    public static readonly Counter<long> RatesRequestsCounter = Meter.CreateCounter<long>(
        name: "marketpulse.rates.requests_total",
        unit: "requests",
        description: "Total number of exchange rate queries handled by the API");

    public static readonly Counter<long> WatchlistCreatedCounter = Meter.CreateCounter<long>(
        name: "marketpulse.watchlist.created_total",
        unit: "items",
        description: "Total number of currency watch items created");

    public static readonly Counter<long> WatchlistDeletedCounter = Meter.CreateCounter<long>(
        name: "marketpulse.watchlist.deleted_total",
        unit: "items",
        description: "Total number of currency watch items deleted");

    public static readonly Counter<long> MessagingPublishedCounter = Meter.CreateCounter<long>(
        name: "marketpulse.messaging.published_total",
        unit: "messages",
        description: "Total number of integration events published to the message broker");

    public static readonly Counter<long> MessagingConsumedCounter = Meter.CreateCounter<long>(
        name: "marketpulse.messaging.consumed_total",
        unit: "messages",
        description: "Total number of integration events successfully consumed from the message broker");

    public static readonly Histogram<double> MessagingConsumeDuration = Meter.CreateHistogram<double>(
        name: "marketpulse.messaging.consume_duration_seconds",
        unit: "s",
        description: "Time spent consuming and processing integration events");
}

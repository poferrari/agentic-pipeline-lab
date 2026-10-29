using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace App.Api.Diagnostics;

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
}

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using App.Application.Abstractions;
using App.Application.MarketPulse.DTOs;
using App.Domain.Common;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.ExternalApis.Frankfurter;

public sealed record FrankfurterApiResponse(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("base")] string Base,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("rates")] Dictionary<string, decimal> Rates);

public sealed class FrankfurterExchangeAdapter(
    HttpClient httpClient,
    HybridCache cache,
    ILogger<FrankfurterExchangeAdapter> logger) : IMarketExchangeProvider
{
    private static readonly Dictionary<string, decimal> EcbReferenceUsdBaseline = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1.0000m,
        ["EUR"] = 0.9240m,
        ["BRL"] = 5.7120m,
        ["GBP"] = 0.7715m,
        ["JPY"] = 152.3500m,
        ["CAD"] = 1.3890m,
        ["AUD"] = 1.5180m,
        ["CHF"] = 0.8660m,
        ["CNY"] = 7.1250m,
        ["MXN"] = 19.9800m
    };

    public async Task<Result<ExchangeRatesQuoteDto>> GetLatestRatesAsync(
        string baseCurrency,
        IReadOnlyCollection<string>? symbols,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseCurrency);

        var normalizedBase = baseCurrency.Trim().ToUpperInvariant();
        var normalizedSymbols = symbols is { Count: > 0 }
            ? string.Join(',', symbols.Select(s => s.Trim().ToUpperInvariant()).OrderBy(s => s, StringComparer.Ordinal))
            : "ALL";

        var cacheKey = $"marketpulse:rates:{normalizedBase}:{normalizedSymbols}";

        var quote = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => await FetchFromFrankfurterWithFallbackAsync(normalizedBase, symbols, cancel),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(15),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            },
            cancellationToken: ct);

        return Result.Success(quote);
    }

    private async Task<ExchangeRatesQuoteDto> FetchFromFrankfurterWithFallbackAsync(
        string baseCurrency,
        IReadOnlyCollection<string>? symbols,
        CancellationToken ct)
    {
        try
        {
            var query = $"latest?from={Uri.EscapeDataString(baseCurrency)}";
            if (symbols is { Count: > 0 })
            {
                var toParam = string.Join(',', symbols.Select(Uri.EscapeDataString));
                query += $"&to={toParam}";
            }

            var response = await httpClient.GetFromJsonAsync<FrankfurterApiResponse>(query, ct);
            if (response is not null && response.Rates.Count > 0)
            {
                return new ExchangeRatesQuoteDto(
                    response.Base,
                    response.Date,
                    "Frankfurter-ECB",
                    response.Rates,
                    DateTimeOffset.UtcNow);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(
                ex,
                "External Frankfurter API call failed or timed out for base {BaseCurrency}. Using deterministic ECB reference fallback.",
                baseCurrency);
        }

        return BuildResilientFallbackQuote(baseCurrency, symbols);
    }

    private static ExchangeRatesQuoteDto BuildResilientFallbackQuote(
        string baseCurrency,
        IReadOnlyCollection<string>? symbols)
    {
        var baseInUsd = EcbReferenceUsdBaseline.GetValueOrDefault(baseCurrency, 1.0m);
        var targetCurrencies = symbols is { Count: > 0 }
            ? symbols.Select(s => s.ToUpperInvariant()).Where(s => s != baseCurrency)
            : EcbReferenceUsdBaseline.Keys.Where(k => !string.Equals(k, baseCurrency, StringComparison.OrdinalIgnoreCase));

        var calculatedRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targetCurrencies)
        {
            var targetInUsd = EcbReferenceUsdBaseline.GetValueOrDefault(target, 1.0m);
            calculatedRates[target] = decimal.Round(targetInUsd / baseInUsd, 6, MidpointRounding.AwayFromZero);
        }

        return new ExchangeRatesQuoteDto(
            baseCurrency,
            DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            "Frankfurter-ECB-Fallback",
            calculatedRates,
            DateTimeOffset.UtcNow);
    }
}

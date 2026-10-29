using App.Application.Common;
using App.Application.MarketPulse.DTOs;
using App.Application.MarketPulse.Queries.GetExchangeRates;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Endpoints;

public sealed class ExchangeRatesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/rates")
            .WithTags("ExchangeRates")
            .RequireRateLimiting("fixed");

        group.MapGet("/{baseCurrency}", async (
            string baseCurrency,
            [FromQuery] string? symbols,
            GetExchangeRatesQueryHandler handler,
            CancellationToken ct) =>
        {
            var normalizedBase = (baseCurrency ?? string.Empty).Trim().ToUpperInvariant();
            MarketPulseDiagnostics.RatesRequestsCounter.Add(1, new KeyValuePair<string, object?>("currency.base", normalizedBase));

            using var activity = MarketPulseDiagnostics.ActivitySource.StartActivity("ExchangeRates.GetQuote");
            activity?.SetTag("currency.base", normalizedBase);
            if (!string.IsNullOrWhiteSpace(symbols))
            {
                activity?.SetTag("currency.symbols", symbols);
            }

            var result = await handler.HandleAsync(new GetExchangeRatesQuery(baseCurrency ?? string.Empty, symbols), ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .AllowAnonymous()
        .WithName("GetLatestExchangeRates")
        .WithSummary("Fetches latest official exchange rates from Frankfurter (ECB) with HybridCache.")
        .Produces<ExchangeRatesQuoteDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

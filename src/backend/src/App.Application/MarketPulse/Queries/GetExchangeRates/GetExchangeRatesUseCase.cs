using App.Application.Abstractions;
using App.Application.MarketPulse.DTOs;
using App.Domain.Common;
using App.Domain.MarketPulse.ValueObjects;
using FluentValidation;

namespace App.Application.MarketPulse.Queries.GetExchangeRates;

public sealed record GetExchangeRatesQuery(string BaseCurrency, string? Symbols);

public sealed class GetExchangeRatesQueryValidator : AbstractValidator<GetExchangeRatesQuery>
{
    public GetExchangeRatesQueryValidator()
    {
        RuleFor(x => x.BaseCurrency)
            .NotEmpty().WithMessage("BaseCurrency is required.")
            .Length(3).WithMessage("BaseCurrency must be a 3-letter ISO code.")
            .Must(CurrencyCode.IsValid)
            .WithMessage("BaseCurrency must be a valid 3-letter ISO-4217 currency code.");
    }
}

public sealed class GetExchangeRatesQueryHandler(
    IMarketExchangeProvider exchangeProvider,
    IValidator<GetExchangeRatesQuery> validator)
{
    public async Task<Result<ExchangeRatesQuoteDto>> HandleAsync(
        GetExchangeRatesQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<ExchangeRatesQuoteDto>(
                Error.Validation("Currency.InvalidCode", validation.Errors[0].ErrorMessage));
        }

        var currencyResult = CurrencyCode.Create(query.BaseCurrency);
        if (currencyResult.IsFailure)
        {
            return Result.Failure<ExchangeRatesQuoteDto>(currencyResult.Error);
        }

        var baseCode = currencyResult.Value.Value;
        IReadOnlyCollection<string>? parsedSymbols = null;

        if (!string.IsNullOrWhiteSpace(query.Symbols))
        {
            var uniqueSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rawTokens = query.Symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var raw in rawTokens)
            {
                var symbolCheck = CurrencyCode.Create(raw);
                if (symbolCheck.IsFailure)
                {
                    return Result.Failure<ExchangeRatesQuoteDto>(symbolCheck.Error);
                }

                if (!string.Equals(symbolCheck.Value.Value, baseCode, StringComparison.OrdinalIgnoreCase))
                {
                    uniqueSymbols.Add(symbolCheck.Value.Value);
                }
            }

            // Edge case: User explicitly requested symbols, but all of them were identical to base currency
            if (rawTokens.Length > 0 && uniqueSymbols.Count == 0)
            {
                return Result.Success(new ExchangeRatesQuoteDto(
                    baseCode,
                    DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    "Internal-SameCurrency",
                    new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [baseCode] = 1.0m },
                    DateTimeOffset.UtcNow));
            }

            parsedSymbols = uniqueSymbols;
        }

        return await exchangeProvider.GetLatestRatesAsync(baseCode, parsedSymbols, ct);
    }
}

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
            .NotEmpty()
            .Length(3)
            .Must(code => CurrencyCode.Create(code).IsSuccess)
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

        IReadOnlyCollection<string>? parsedSymbols = null;
        if (!string.IsNullOrWhiteSpace(query.Symbols))
        {
            var list = new List<string>();
            foreach (var raw in query.Symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var symbolCheck = CurrencyCode.Create(raw);
                if (symbolCheck.IsFailure)
                {
                    return Result.Failure<ExchangeRatesQuoteDto>(symbolCheck.Error);
                }

                if (symbolCheck.Value != currencyResult.Value)
                {
                    list.Add(symbolCheck.Value.Value);
                }
            }

            parsedSymbols = list;
        }

        return await exchangeProvider.GetLatestRatesAsync(currencyResult.Value.Value, parsedSymbols, ct);
    }
}

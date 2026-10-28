using App.Domain.Common;
using App.Domain.MarketPulse.Errors;

namespace App.Domain.MarketPulse.ValueObjects;

public readonly record struct CurrencyCode
{
    private static readonly HashSet<string> SupportedIsoCurrencies = new(StringComparer.Ordinal)
    {
        "USD", "EUR", "BRL", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "MXN",
        "SEK", "NOK", "DKK", "PLN", "CZK", "HUF", "TRY", "ZAR", "SGD", "HKD",
        "NZD", "KRW", "INR", "IDR", "MYR", "PHP", "THB", "ILS", "ISK", "BGN", "RON"
    };

    public string Value { get; }

    private CurrencyCode(string value)
    {
        Value = value;
    }

    public static Result<CurrencyCode> Create(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return Result.Failure<CurrencyCode>(MarketPulseErrors.InvalidCurrencyCode(rawCode));
        }

        var normalized = rawCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || !SupportedIsoCurrencies.Contains(normalized))
        {
            return Result.Failure<CurrencyCode>(MarketPulseErrors.InvalidCurrencyCode(rawCode));
        }

        return Result.Success(new CurrencyCode(normalized));
    }

    public override string ToString() => Value;
}

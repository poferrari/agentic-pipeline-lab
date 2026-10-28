using App.Domain.Common;

namespace App.Domain.MarketPulse.Errors;

public static class MarketPulseErrors
{
    public static Error InvalidCurrencyCode(string? code) =>
        Error.Validation("Currency.InvalidCode", $"The currency code '{code}' is invalid. Must be a supported 3-letter ISO-4217 code.");

    public static readonly Error InvalidTargetRate =
        Error.Validation("WatchItem.InvalidTargetRate", "The target exchange rate must be strictly greater than zero and at most 1,000,000.");

    public static readonly Error InvalidOwner =
        Error.Validation("WatchItem.InvalidOwner", "The watch item owner identifier cannot be empty.");

    public static readonly Error SameCurrencyNotAllowed =
        Error.Validation("WatchItem.SameCurrencyNotAllowed", "Base currency and target currency cannot be identical.");

    public static readonly Error NotesTooLong =
        Error.Validation("WatchItem.NotesTooLong", "Watch item notes cannot exceed 250 characters.");

    public static Error DuplicatePair(string baseCurrency, string targetCurrency) =>
        Error.Conflict("WatchItem.DuplicatePair", $"An active watch item for pair {baseCurrency}/{targetCurrency} already exists for this user.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("WatchItem.NotFound", $"Currency watch item with id '{id}' was not found.");
}

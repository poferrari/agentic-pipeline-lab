using App.Domain.Common;
using App.Domain.MarketPulse.Errors;

namespace App.Domain.MarketPulse.ValueObjects;

public readonly record struct TargetRate
{
    public const decimal MaxAllowedRate = 1_000_000m;

    public decimal Value { get; }

    private TargetRate(decimal value)
    {
        Value = decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }

    public static Result<TargetRate> Create(decimal value)
    {
        if (value <= 0m || value > MaxAllowedRate)
        {
            return Result.Failure<TargetRate>(MarketPulseErrors.InvalidTargetRate);
        }

        return Result.Success(new TargetRate(value));
    }
}

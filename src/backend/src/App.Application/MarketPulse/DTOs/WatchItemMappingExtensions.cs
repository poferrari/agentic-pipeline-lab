using App.Domain.MarketPulse.Entities;

namespace App.Application.MarketPulse.DTOs;

public static class WatchItemMappingExtensions
{
    public static WatchItemDto ToDto(this CurrencyWatchItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var currentRate = item.LastKnownRate;
        var spread = currentRate.HasValue ? item.CalculateSpreadPercentage(currentRate.Value) : 0m;

        return new WatchItemDto(
            item.Id,
            item.OwnerId,
            item.BaseCurrency.Value,
            item.TargetCurrency.Value,
            item.TargetRate.Value,
            currentRate,
            spread,
            item.NotifyWhenAbove,
            item.IsTriggered,
            item.Notes,
            item.CreatedAtUtc,
            item.TriggeredAtUtc);
    }
}

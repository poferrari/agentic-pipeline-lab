using App.Domain.MarketPulse.Entities;
using App.Domain.MarketPulse.ValueObjects;

namespace App.Domain.MarketPulse.Repositories;

public interface ICurrencyWatchItemRepository
{
    Task<CurrencyWatchItem?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task<bool> ExistsByPairAsync(Guid ownerId, CurrencyCode baseCurrency, CurrencyCode targetCurrency, CancellationToken ct = default);
    Task<(IReadOnlyList<CurrencyWatchItem> Items, int TotalCount)> GetPagedByOwnerAsync(Guid ownerId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(CurrencyWatchItem item, CancellationToken ct = default);
    void Remove(CurrencyWatchItem item);
}

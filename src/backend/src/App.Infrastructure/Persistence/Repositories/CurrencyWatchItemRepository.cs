using App.Domain.MarketPulse.Entities;
using App.Domain.MarketPulse.Repositories;
using App.Domain.MarketPulse.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Persistence.Repositories;

public sealed class CurrencyWatchItemRepository(AppDbContext dbContext) : ICurrencyWatchItemRepository
{
    public async Task<CurrencyWatchItem?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        return await dbContext.CurrencyWatchItems
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId, ct);
    }

    public async Task<bool> ExistsByPairAsync(
        Guid ownerId,
        CurrencyCode baseCurrency,
        CurrencyCode targetCurrency,
        CancellationToken ct = default)
    {
        return await dbContext.CurrencyWatchItems
            .AsNoTracking()
            .AnyAsync(
                x => x.OwnerId == ownerId &&
                     x.BaseCurrency == baseCurrency &&
                     x.TargetCurrency == targetCurrency,
                ct);
    }

    public async Task<(IReadOnlyList<CurrencyWatchItem> Items, int TotalCount)> GetPagedByOwnerAsync(
        Guid ownerId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = dbContext.CurrencyWatchItems
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(CurrencyWatchItem item, CancellationToken ct = default)
    {
        await dbContext.CurrencyWatchItems.AddAsync(item, ct);
    }

    public void Remove(CurrencyWatchItem item)
    {
        dbContext.CurrencyWatchItems.Remove(item);
    }
}

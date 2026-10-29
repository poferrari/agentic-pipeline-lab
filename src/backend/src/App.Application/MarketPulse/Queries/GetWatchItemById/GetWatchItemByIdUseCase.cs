using App.Application.Abstractions;
using App.Application.MarketPulse.DTOs;
using App.Domain.Common;
using App.Domain.MarketPulse.Errors;
using App.Domain.MarketPulse.Repositories;

namespace App.Application.MarketPulse.Queries.GetWatchItemById;

public sealed record GetWatchItemByIdQuery(Guid Id);

public sealed class GetWatchItemByIdQueryHandler(
    ICurrencyWatchItemRepository repository,
    ICurrentUserProvider currentUserProvider)
{
    public async Task<Result<WatchItemDto>> HandleAsync(
        GetWatchItemByIdQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var ownerId = currentUserProvider.GetCurrentUserId();
        if (ownerId == Guid.Empty)
        {
            return Result.Failure<WatchItemDto>(MarketPulseErrors.InvalidOwner);
        }

        var item = await repository.GetByIdAsync(query.Id, ownerId, ct);
        if (item is null)
        {
            return Result.Failure<WatchItemDto>(MarketPulseErrors.NotFound(query.Id));
        }

        return Result.Success(item.ToDto());
    }
}

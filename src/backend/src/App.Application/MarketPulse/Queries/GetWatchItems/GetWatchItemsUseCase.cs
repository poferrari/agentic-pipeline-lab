using App.Application.Abstractions;
using App.Application.MarketPulse.DTOs;
using App.Domain.Common;
using App.Domain.MarketPulse.Errors;
using App.Domain.MarketPulse.Repositories;
using FluentValidation;

namespace App.Application.MarketPulse.Queries.GetWatchItems;

public sealed record GetWatchItemsQuery(int Page = 1, int PageSize = 20);

public sealed class GetWatchItemsQueryValidator : AbstractValidator<GetWatchItemsQuery>
{
    public GetWatchItemsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetWatchItemsQueryHandler(
    ICurrencyWatchItemRepository repository,
    ICurrentUserProvider currentUserProvider,
    IValidator<GetWatchItemsQuery> validator)
{
    public async Task<Result<PagedResult<WatchItemDto>>> HandleAsync(
        GetWatchItemsQuery query,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<PagedResult<WatchItemDto>>(
                Error.Validation("Pagination.Invalid", validation.Errors[0].ErrorMessage));
        }

        var ownerId = currentUserProvider.GetCurrentUserId();
        if (ownerId == Guid.Empty)
        {
            return Result.Failure<PagedResult<WatchItemDto>>(MarketPulseErrors.InvalidOwner);
        }

        var (items, totalCount) = await repository.GetPagedByOwnerAsync(ownerId, query.Page, query.PageSize, ct);
        var dtos = items.Select(x => x.ToDto()).ToList();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return Result.Success(new PagedResult<WatchItemDto>(dtos, query.Page, query.PageSize, totalCount, totalPages));
    }
}

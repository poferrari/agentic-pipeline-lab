using App.Application.Abstractions;
using App.Domain.Common;
using App.Domain.MarketPulse.Errors;
using App.Domain.MarketPulse.Repositories;

namespace App.Application.MarketPulse.Commands.DeleteWatchItem;

public sealed record DeleteWatchItemCommand(Guid Id);

public sealed class DeleteWatchItemCommandHandler(
    ICurrencyWatchItemRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUserProvider)
{
    public async Task<Result> HandleAsync(
        DeleteWatchItemCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ownerId = currentUserProvider.GetCurrentUserId();
        if (ownerId == Guid.Empty)
        {
            return Result.Failure(MarketPulseErrors.InvalidOwner);
        }

        var item = await repository.GetByIdAsync(command.Id, ownerId, ct);
        if (item is null)
        {
            return Result.Failure(MarketPulseErrors.NotFound(command.Id));
        }

        repository.Remove(item);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}

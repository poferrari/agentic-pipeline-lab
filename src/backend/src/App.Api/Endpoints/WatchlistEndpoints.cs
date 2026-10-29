using App.Api.Diagnostics;
using App.Application.MarketPulse.Commands.CreateWatchItem;
using App.Application.MarketPulse.Commands.DeleteWatchItem;
using App.Application.MarketPulse.DTOs;
using App.Application.MarketPulse.Queries.GetWatchItemById;
using App.Application.MarketPulse.Queries.GetWatchItems;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Endpoints;

public sealed record CreateWatchItemRequest(
    string BaseCurrency,
    string TargetCurrency,
    decimal TargetRate,
    bool NotifyWhenAbove = true,
    string? Notes = null);

public sealed class WatchlistEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/watchlist")
            .WithTags("Watchlist")
            .RequireAuthorization()
            .RequireRateLimiting("fixed");

        group.MapPost("/", async (
            [FromBody] CreateWatchItemRequest request,
            CreateWatchItemCommandHandler handler,
            CancellationToken ct) =>
        {
            using var activity = MarketPulseDiagnostics.ActivitySource.StartActivity("Watchlist.CreateItem");
            activity?.SetTag("currency.base", request.BaseCurrency);
            activity?.SetTag("currency.target", request.TargetCurrency);

            var command = new CreateWatchItemCommand(
                request.BaseCurrency,
                request.TargetCurrency,
                request.TargetRate,
                request.NotifyWhenAbove,
                request.Notes);

            var result = await handler.HandleAsync(command, ct);

            if (result.IsSuccess)
            {
                MarketPulseDiagnostics.WatchlistCreatedCounter.Add(1,
                    new KeyValuePair<string, object?>("currency.base", request.BaseCurrency.ToUpperInvariant()),
                    new KeyValuePair<string, object?>("currency.target", request.TargetCurrency.ToUpperInvariant()));

                return TypedResults.Created($"/api/v1/watchlist/{result.Value.Id}", result.Value);
            }

            return result.ToProblemDetails();
        })
        .WithName("CreateWatchItem")
        .WithSummary("Creates a new currency watch alert for the authenticated user.")
        .Produces<WatchItemDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async (
            GetWatchItemsQueryHandler handler,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await handler.HandleAsync(new GetWatchItemsQuery(page, pageSize), ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithName("GetWatchItems")
        .WithSummary("Lists paginated currency watch alerts owned by the authenticated user.")
        .Produces<PagedResult<WatchItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetWatchItemByIdQueryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetWatchItemByIdQuery(id), ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithName("GetWatchItemById")
        .WithSummary("Gets a specific currency watch alert owned by the authenticated user (BOLA-protected).")
        .Produces<WatchItemDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteWatchItemCommandHandler handler,
            CancellationToken ct) =>
        {
            using var activity = MarketPulseDiagnostics.ActivitySource.StartActivity("Watchlist.DeleteItem");
            activity?.SetTag("watchlist.item_id", id.ToString());

            var result = await handler.HandleAsync(new DeleteWatchItemCommand(id), ct);

            if (result.IsSuccess)
            {
                MarketPulseDiagnostics.WatchlistDeletedCounter.Add(1);
                return TypedResults.NoContent();
            }

            return result.ToProblemDetails();
        })
        .WithName("DeleteWatchItem")
        .WithSummary("Deletes a specific currency watch alert owned by the authenticated user (BOLA-protected).")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

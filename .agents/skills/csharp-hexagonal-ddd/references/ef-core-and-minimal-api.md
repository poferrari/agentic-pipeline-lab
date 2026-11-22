# EF Core Queries, Minimal APIs, and Async Rules

Reference for the `csharp-hexagonal-ddd` skill. Load only when the task touches this area.

## 4. EF Core Query Guardrails (`optimizing-ef-core-queries`)

1. **Pure Read Queries with `.AsNoTracking()`**: Always apply `.AsNoTracking()` on read queries (CQRS queries).
2. **Direct Projections**: Project directly to DTOs via `.Select()` prior to materialization with `.ToListAsync(ct)`. Never load full entities if only specific fields are needed.
3. **No Cartesian Explosion**: When navigating multiple relationships, add `.AsSplitQuery()` to eliminate massive relational cartesian explosions in PostgreSQL.
4. **SQL Traceability**: Add `.TagWith("MarketPulse.GetActiveWatchlist")` on critical queries for instant recognition in `pg_stat_statements` and APMs.
5. **Cancellation Propagation**: Always pass `CancellationToken ct` to EF Core methods (`ToListAsync(ct)`, `FirstOrDefaultAsync(ct)`, `SaveChangesAsync(ct)`).

---

## 5. Clean ASP.NET Core Minimal APIs (.NET 9+)

1. **No Swashbuckle**: Use native .NET 9 OpenAPI (`builder.Services.AddOpenApi()` + `app.MapOpenApi()`).
2. **Strict `TypedResults`**: Map endpoints returning concrete types for clean static schema inference:

```csharp
public sealed class WatchlistEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/watchlist")
            .WithTags("Watchlist")
            .RequireAuthorization();

        group.MapPost("/", CreateItemAsync)
            .WithName("CreateWatchItem")
            .WithSummary("Registers a new currency alert item.");
    }

    private static async Task<Results<CreatedAtRoute<WatchItemResponse>, ProblemHttpResult>> CreateItemAsync(
        CreateWatchItemRequest request,
        ICreateWatchItemUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        if (result.IsFailure)
        {
            return TypedResults.Problem(
                statusCode: result.Error.Type switch
                {
                    ErrorType.Validation => StatusCodes.Status400BadRequest,
                    ErrorType.Conflict   => StatusCodes.Status409Conflict,
                    ErrorType.NotFound   => StatusCodes.Status404NotFound,
                    _                    => StatusCodes.Status500InternalServerError
                },
                title: result.Error.Code,
                detail: result.Error.Description);
        }

        return TypedResults.CreatedAtRoute(result.Value, "GetWatchItemById", new { id = result.Value.Id });
    }
}
```

---

## 6. Anti-Pattern: Prohibition of Sync-Over-Async (`csharp-async`)

- **NEVER** invoke:
  - `task.Result`
  - `task.GetAwaiter().GetResult()`
  - `task.Wait()`
- **Always use `await` and return `Task` / `Task<T>`** (or `ValueTask<T>` on high-frequency synchronous execution paths).
- Propagate `CancellationToken ct` down to HttpClient, database, and broker operations to immediately release resources when client connections terminate.

---

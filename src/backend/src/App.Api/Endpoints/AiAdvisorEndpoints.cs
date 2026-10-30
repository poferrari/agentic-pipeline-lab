using App.Application.MarketPulse.Queries.AskAiAdvisor;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Endpoints;

public sealed class AiAdvisorEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai")
            .WithTags("AiAdvisor")
            .RequireRateLimiting("fixed");

        group.MapPost("/chat", async (
            [FromBody] AskAiAdvisorRequest request,
            AskAiAdvisorQueryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .AllowAnonymous()
        .WithName("AskAiAdvisor")
        .WithSummary("Interactive contextual AI advisor for MarketPulse FX rates, alerts, and Hexagonal Architecture.")
        .WithDescription("Accepts user queries and chat history, grounding answers in real-time FX rates. Questions outside the MVP domain are politely declined.")
        .Produces<AiAdvisorResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}


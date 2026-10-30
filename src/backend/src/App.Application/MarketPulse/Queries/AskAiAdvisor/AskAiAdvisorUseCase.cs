using App.Application.Abstractions;
using App.Domain.Common;
using App.Domain.MarketPulse.Ports;
using FluentValidation;

namespace App.Application.MarketPulse.Queries.AskAiAdvisor;

public sealed record AskAiAdvisorMessageDto(string Role, string Content);

public sealed record AskAiAdvisorRequest(
    string Message,
    IReadOnlyList<AskAiAdvisorMessageDto>? History = null);

public sealed record AiAdvisorResponseDto(
    string Reply,
    IReadOnlyList<string> SuggestedActions,
    string ModelUsed,
    DateTimeOffset GeneratedAtUtc);

public sealed class AskAiAdvisorValidator : AbstractValidator<AskAiAdvisorRequest>
{
    public AskAiAdvisorValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(500).WithMessage("Message cannot exceed 500 characters.");
    }
}

public sealed class AskAiAdvisorQueryHandler(
    IChatAdvisor chatAdvisor,
    IMarketExchangeProvider exchangeProvider,
    IValidator<AskAiAdvisorRequest> validator)
{
    public async Task<Result<AiAdvisorResponseDto>> HandleAsync(
        AskAiAdvisorRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<AiAdvisorResponseDto>(
                Error.Validation("Ai.ValidationFailed", validation.Errors[0].ErrorMessage));
        }

        // Ground the AI context with the latest real-time rates from Frankfurter / HybridCache
        var liveRatesResult = await exchangeProvider.GetLatestRatesAsync("USD", ["BRL", "EUR", "GBP", "JPY"], ct);
        var marketContext = liveRatesResult.IsSuccess
            ? $"Current ECB Reference Rates (Base USD): 1 USD = {string.Join(", ", liveRatesResult.Value.Rates.Select(r => $"{r.Value:F4} {r.Key}"))}."
            : "Current market rates are actively cached and monitored.";

        var domainHistory = request.History?
            .Select(h => new AiChatMessage(h.Role, h.Content))
            .ToList();

        var aiResult = await chatAdvisor.GetAdviceAsync(
            request.Message.Trim(),
            domainHistory,
            marketContext,
            ct);

        if (aiResult.IsFailure)
        {
            return Result.Failure<AiAdvisorResponseDto>(aiResult.Error);
        }

        return Result.Success(new AiAdvisorResponseDto(
            aiResult.Value.Reply,
            aiResult.Value.SuggestedActions,
            aiResult.Value.ModelUsed,
            DateTimeOffset.UtcNow));
    }
}

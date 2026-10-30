using App.Domain.Common;

namespace App.Domain.MarketPulse.Ports;

public sealed record AiChatMessage(string Role, string Content);

public sealed record AiAdvisorResult(
    string Reply,
    IReadOnlyList<string> SuggestedActions,
    string ModelUsed);

/// <summary>
/// Outbound Port interface for AI Contextual Advisor.
/// Implemented by Infrastructure adapters (e.g. Semantic Kernel / Microsoft.Extensions.AI / OpenAI).
/// </summary>
public interface IChatAdvisor
{
    Task<Result<AiAdvisorResult>> GetAdviceAsync(
        string userPrompt,
        IReadOnlyList<AiChatMessage>? history,
        string contextualMarketData,
        CancellationToken cancellationToken = default);
}


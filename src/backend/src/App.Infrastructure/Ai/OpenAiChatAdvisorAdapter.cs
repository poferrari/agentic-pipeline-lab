using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using App.Domain.Common;
using App.Domain.MarketPulse.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Ai;

/// <summary>
/// Infrastructure adapter implementing IChatAdvisor via OpenAI-compatible REST API
/// (supports OpenAI, Groq, OpenRouter, Azure OpenAI, or local Ollama)
/// with strict domain boundary guardrails and zero-cost deterministic fallback.
/// </summary>
public sealed class OpenAiChatAdvisorAdapter : IChatAdvisor
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiChatAdvisorAdapter> _logger;

    private const string SystemPrompt =
        "You are the MarketPulse AI Advisor, an expert assistant for the MarketPulse Lab platform.\n" +
        "Your ONLY responsibility is to assist users with:\n" +
        "1. Real-time foreign exchange (FX) rates, currency pairs, and rate interpretations.\n" +
        "2. Currency threshold alerts and the Transactional Outbox Pattern in MarketPulse.\n" +
        "3. The platform's software architecture: ASP.NET Core 9 Minimal API, Hexagonal Architecture (Ports & Adapters), DDD, PostgreSQL, RabbitMQ, HybridCache, and Supabase Auth.\n\n" +
        "STRICT CONSTRAINTS & GUARDRAILS:\n" +
        "- You must ONLY answer questions directly related to foreign exchange, currency conversions, MarketPulse alerts, and this platform's architecture.\n" +
        "- If a user asks about anything outside this domain (such as general knowledge, cooking, politics, sports, entertainment, unrelated programming tasks, or hacking), you MUST politely refuse with:\n" +
        "  \"I am the MarketPulse AI Advisor specialized strictly in foreign exchange rates, currency alerts, and this platform's architecture. I cannot assist with topics outside this scope. Please ask me about FX rates, currency alerts, or our Hexagonal Architecture.\"\n" +
        "- Ground your answers using the provided current market data when available.\n" +
        "- Keep responses concise, professional, and actionable.";

    public OpenAiChatAdvisorAdapter(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiChatAdvisorAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AiAdvisorResult>> GetAdviceAsync(
        string userPrompt,
        IReadOnlyList<AiChatMessage>? history,
        string contextualMarketData,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return Result.Failure<AiAdvisorResult>(
                Error.Validation("Ai.EmptyPrompt", "User prompt cannot be empty."));
        }

        var apiKey = _configuration["Ai:ApiKey"]
            ?? _configuration["AI__ApiKey"]
            ?? _configuration["OPENAI_API_KEY"];

        var modelId = _configuration["Ai:ModelId"] ?? "gpt-4o-mini";
        var endpoint = _configuration["Ai:Endpoint"] ?? "https://api.openai.com/v1";

        // If no secret key is provided (e.g. CI/local dev without LLM billing), execute deterministic domain engine
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("No Ai:ApiKey configured. Running MarketPulse deterministic domain advisor engine.");
            return Result.Success(EvaluateDeterministicEngine(userPrompt, contextualMarketData, "rule-engine-domain-fallback"));
        }

        try
        {
            var messages = new List<ChatCompletionMessageDto>
            {
                new("system", $"{SystemPrompt}\n\n[Current Grounded Market Data]\n{contextualMarketData}")
            };

            if (history is { Count: > 0 })
            {
                foreach (var h in history.TakeLast(6))
                {
                    var role = h.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
                    messages.Add(new(role, h.Content));
                }
            }

            messages.Add(new("user", userPrompt));

            var requestBody = new ChatCompletionRequestDto(
                Model: modelId,
                Messages: messages,
                Temperature: 0.2f,
                MaxTokens: 500);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint.TrimEnd('/')}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(requestBody);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("LLM Provider returned HTTP {StatusCode}: {ErrorBody}. Falling back to domain engine.",
                    response.StatusCode, errorBody);
                return Result.Success(EvaluateDeterministicEngine(userPrompt, contextualMarketData, $"{modelId} (fallback)"));
            }

            var resultDto = await response.Content.ReadFromJsonAsync<ChatCompletionResponseDto>(
                cancellationToken: cancellationToken);

            var reply = (resultDto?.Choices is { Count: > 0 } choices ? choices[0].Message.Content?.Trim() : null);
            if (string.IsNullOrWhiteSpace(reply))
            {
                return Result.Success(EvaluateDeterministicEngine(userPrompt, contextualMarketData, modelId));
            }

            var suggestedActions = ExtractSuggestedActions(reply);
            return Result.Success(new AiAdvisorResult(reply, suggestedActions, modelId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception communicating with LLM API. Falling back to domain advisor engine.");
            return Result.Success(EvaluateDeterministicEngine(userPrompt, contextualMarketData, "rule-engine-domain-fallback"));
        }
    }

    private static AiAdvisorResult EvaluateDeterministicEngine(
        string prompt,
        string contextualMarketData,
        string modelUsed)
    {
        var lower = prompt.ToLowerInvariant();

        // Check for out-of-scope triggers (recipes, sports, weather, unrelated general programming, politics)
        string[] outOfScopeKeywords =
        [
            "recipe", "cook", "cake", "food", "weather", "rain", "messi", "ronaldo", "football", "soccer",
            "president", "election", "movie", "song", "poem", "joke", "capital of", "who is", "game",
            "python", "javascript", "rust", "c++", "write a script", "hack"
        ];

        bool isOffTopic = outOfScopeKeywords.Any(k => lower.Contains(k))
            && !lower.Contains("marketpulse")
            && !lower.Contains("rate")
            && !lower.Contains("currency");

        if (isOffTopic)
        {
            return new AiAdvisorResult(
                "I am the MarketPulse AI Advisor specialized strictly in foreign exchange rates, currency alerts, and this platform's architecture. I cannot assist with topics outside this scope. Please ask me about FX rates, currency alerts, or our Hexagonal Architecture.",
                ["Check USD/BRL rate", "How do currency alerts work?", "Explain Hexagonal Architecture"],
                modelUsed);
        }

        // In-scope responses
        if (lower.Contains("architecture") || lower.Contains("hexagonal") || lower.Contains("ddd") || lower.Contains("pattern"))
        {
            return new AiAdvisorResult(
                "MarketPulse Lab is built with Hexagonal Architecture (Ports & Adapters) and DDD in .NET 9:\n" +
                "• Domain: Isolated business rules (Entities, Value Objects, Outbound Ports) with zero framework dependencies.\n" +
                "• Application: CQRS Handlers, FluentValidation, and Result Pattern.\n" +
                "• Infrastructure: EF Core (PostgreSQL), MassTransit Transactional Outbox (RabbitMQ), HybridCache L1/L2, and Supabase Auth.\n" +
                "• API: High-throughput ASP.NET Core Minimal APIs with RFC 7807 ProblemDetails.",
                ["View Live Rates", "How do Outbox alerts work?", "Explain Supabase Auth integration"],
                modelUsed);
        }

        if (lower.Contains("alert") || lower.Contains("outbox") || lower.Contains("rabbitmq") || lower.Contains("threshold"))
        {
            return new AiAdvisorResult(
                "Currency Watchlist Alerts leverage the Transactional Outbox Pattern:\n" +
                "1. When an alert is created or triggered, the state and the event are persisted in PostgreSQL in the same database transaction.\n" +
                "2. MassTransit Outbox dispatches the event asynchronously to RabbitMQ (CloudAMQP) with guaranteed at-least-once delivery.\n" +
                "3. Consumer workers process the alert notification without dual-write inconsistency risks.",
                ["Create a Watchlist Alert", "Check USD/EUR rates", "View Architecture summary"],
                modelUsed);
        }

        if (lower.Contains("supabase") || lower.Contains("auth") || lower.Contains("jwt") || lower.Contains("security"))
        {
            return new AiAdvisorResult(
                "MarketPulse delegates authentication to Supabase Auth. The C# Minimal API validates incoming Bearer JWT tokens via asymmetric JWKS (ES256/RS256) signature verification. In addition, PostgreSQL Row-Level Security (RLS) and Application-layer user isolation ensure strict tenant data segregation.",
                ["Check Watchlist", "Explore Live Rates", "Explain System Architecture"],
                modelUsed);
        }

        // Default FX Rates & General In-Scope Assistance
        return new AiAdvisorResult(
            $"Welcome to MarketPulse! {contextualMarketData}\n\n" +
            "You can monitor live European Central Bank reference quotes, set threshold alerts with guaranteed MassTransit Outbox delivery, or inspect our distributed .NET 9 Hexagonal Architecture.",
            ["What is the current USD/EUR rate?", "How do Outbox alerts work?", "Explain Hexagonal Architecture"],
            modelUsed);
    }

    private static IReadOnlyList<string> ExtractSuggestedActions(string reply)
    {
        if (reply.Contains("outside this scope", StringComparison.OrdinalIgnoreCase))
        {
            return ["Check USD/BRL rate", "How do currency alerts work?", "Explain Hexagonal Architecture"];
        }

        return
        [
            "What is the current USD/EUR rate?",
            "How does the Outbox Pattern prevent dual-writes?",
            "Explain Hexagonal Architecture"
        ];
    }

    private sealed record ChatCompletionMessageDto(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionRequestDto(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatCompletionMessageDto> Messages,
        [property: JsonPropertyName("temperature")] float Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private sealed record ChatCompletionChoiceDto(
        [property: JsonPropertyName("message")] ChatCompletionMessageDto Message);

    private sealed record ChatCompletionResponseDto(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChoiceDto>? Choices);
}

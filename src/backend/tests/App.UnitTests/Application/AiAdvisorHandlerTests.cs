using App.Application.Abstractions;
using App.Application.MarketPulse.DTOs;
using App.Application.MarketPulse.Queries.AskAiAdvisor;
using App.Domain.Common;
using App.Domain.MarketPulse.Ports;
using App.Infrastructure.Ai;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace App.UnitTests.Application;

public sealed class AiAdvisorHandlerTests
{
    private readonly IChatAdvisor _chatAdvisor = Substitute.For<IChatAdvisor>();
    private readonly IMarketExchangeProvider _exchangeProvider = Substitute.For<IMarketExchangeProvider>();
    private readonly AskAiAdvisorValidator _validator = new();

    private AskAiAdvisorQueryHandler CreateHandler() =>
        new(_chatAdvisor, _exchangeProvider, _validator);

    [Fact]
    public async Task HandleAsync_WhenRequestIsValid_ReturnsSuccessfulAdvice()
    {
        // Arrange
        var rates = new Dictionary<string, decimal> { { "BRL", 5.85m }, { "EUR", 0.92m } };
        _exchangeProvider.GetLatestRatesAsync("USD", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ExchangeRatesQuoteDto("USD", "2026-10-30", "Frankfurter (ECB)", rates, DateTimeOffset.UtcNow)));

        _chatAdvisor.GetAdviceAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<AiChatMessage>?>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success(new AiAdvisorResult("Here is your FX insight", ["Check USD/BRL"], "gpt-4o-mini")));

        var handler = CreateHandler();
        var request = new AskAiAdvisorRequest("What is the current USD to EUR rate?");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Reply.Should().Be("Here is your FX insight");
        result.Value.ModelUsed.Should().Be("gpt-4o-mini");
        result.Value.SuggestedActions.Should().ContainSingle().Which.Should().Be("Check USD/BRL");
    }

    [Fact]
    public async Task HandleAsync_WhenMessageIsEmpty_ReturnsValidationError()
    {
        // Arrange
        var handler = CreateHandler();
        var request = new AskAiAdvisorRequest(string.Empty);

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ai.ValidationFailed");
    }

    [Fact]
    public async Task HandleAsync_WhenMessageExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var handler = CreateHandler();
        var request = new AskAiAdvisorRequest(new string('a', 501));

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ai.ValidationFailed");
    }

    [Fact]
    public async Task OpenAiChatAdvisorAdapter_WithoutApiKey_RejectsOffTopicQueryPolitely()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build(); // No Ai:ApiKey
        using var httpClient = new HttpClient();
        var adapter = new OpenAiChatAdvisorAdapter(httpClient, config, NullLogger<OpenAiChatAdvisorAdapter>.Instance);

        // Act
        var result = await adapter.GetAdviceAsync("Can you give me a recipe for chocolate cake?", null, "No market data");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Reply.Should().Contain("outside this scope");
        result.Value.SuggestedActions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OpenAiChatAdvisorAdapter_WithoutApiKey_AnswersArchitectureQuery()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        using var httpClient = new HttpClient();
        var adapter = new OpenAiChatAdvisorAdapter(httpClient, config, NullLogger<OpenAiChatAdvisorAdapter>.Instance);

        // Act
        var result = await adapter.GetAdviceAsync("Explain the Hexagonal Architecture and DDD layers", null, "Context data");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Reply.Should().Contain("Hexagonal Architecture (Ports & Adapters)");
        result.Value.Reply.Should().Contain("MassTransit Transactional Outbox");
    }
}

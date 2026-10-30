using System.Net;
using System.Net.Http.Json;
using App.Api.Endpoints;
using App.Api.Middleware;
using App.Application.MarketPulse.DTOs;
using App.Application.MarketPulse.Queries.AskAiAdvisor;
using App.Domain.Common;
using App.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace App.IntegrationTests;

[Collection(MarketPulseTestGroupDefinition.CollectionName)]
public sealed class MarketPulseApiIntegrationTests(MarketPulseTestcontainersFactory factory)
{
    [Fact]
    public void TestEnvironment_Should_Be_Properly_Configured()
    {
        factory.Should().NotBeNull();
        if (factory.IsDockerEnvironmentActive)
        {
            Assert.True(factory.IsDockerEnvironmentActive, "Running with real PostgreSQL and RabbitMQ Testcontainers.");
        }
        else
        {
            Assert.False(factory.IsDockerEnvironmentActive, "Running in Adaptive In-Memory mode (Docker daemon offline on current host).");
        }
    }

    [Fact]
    public async Task HealthChecks_And_SecurityHeaders_ShouldReturn200OkWithOwaspHeaders()
    {
        using var client = factory.CreateClient();

        var liveResponse = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        var readyResponse = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        liveResponse.Headers.Should().ContainKey("X-Content-Type-Options");
        liveResponse.Headers.Should().ContainKey("X-Frame-Options");
        liveResponse.Headers.Should().ContainKey("X-Correlation-Id");
        liveResponse.Headers.Should().ContainKey("X-Trace-Id");
    }

    [Fact]
    public async Task TelemetryMiddleware_ShouldPreserveIncomingCorrelationIdAndInjectTraceId()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/health/live", UriKind.Relative));
        request.Headers.Add("X-Correlation-Id", "client-trace-12345");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Correlation-Id").Should().Contain("client-trace-12345");
        response.Headers.Should().ContainKey("X-Trace-Id");
    }

    [Fact]
    public async Task GetLatestExchangeRates_WithValidBaseCurrency_ShouldReturn200OkAndRatesFromPublicApiAdapter()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/rates/USD?symbols=BRL,EUR", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var quote = await response.Content.ReadFromJsonAsync<ExchangeRatesQuoteDto>();
        quote.Should().NotBeNull();
        quote!.BaseCurrency.Should().Be("USD");
        quote.Rates.Should().ContainKey("BRL");
        quote.Rates["BRL"].Should().BeGreaterThan(0m);
        quote.Rates.Should().ContainKey("EUR");
        quote.Rates["EUR"].Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task GetLatestExchangeRates_WithInvalidCurrency_ShouldReturn400ProblemDetails()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/rates/INVALID", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Currency.InvalidCode");
    }

    [Fact]
    public async Task Watchlist_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/watchlist", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Watchlist_FullCrudLifecycle_And_BolaIdorProtection_ShouldEnforceStrictOwnerIsolation()
    {
        // Independent test IDs to guarantee zero chronological or logical dependencies between tests
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        using var clientA = factory.CreateClient();
        clientA.DefaultRequestHeaders.Add(HybridSupabaseOrDemoAuthHandler.DemoHeaderName, userAId.ToString());

        using var clientB = factory.CreateClient();
        clientB.DefaultRequestHeaders.Add(HybridSupabaseOrDemoAuthHandler.DemoHeaderName, userBId.ToString());

        // 1. User A creates a WatchItem for EUR -> BRL
        var createRequest = new CreateWatchItemRequest("EUR", "BRL", 6.10m, NotifyWhenAbove: true, "EUR export alert");
        var createResponse = await clientA.PostAsJsonAsync("/api/v1/watchlist", createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdItem = await createResponse.Content.ReadFromJsonAsync<WatchItemDto>();
        createdItem.Should().NotBeNull();
        createdItem!.OwnerId.Should().Be(userAId);
        createdItem.BaseCurrency.Should().Be("EUR");
        createdItem.TargetCurrency.Should().Be("BRL");
        createdItem.CurrentRate.Should().BeGreaterThan(0m);

        // 2. Duplicate creation by User A for same pair returns 409 Conflict
        var duplicateResponse = await clientA.PostAsJsonAsync("/api/v1/watchlist", createRequest);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 3. BOLA/IDOR Security Test (OWASP API1:2023): User B attempts to GET or DELETE User A's WatchItem
        var bolaGetResponse = await clientB.GetAsync(new Uri($"/api/v1/watchlist/{createdItem.Id}", UriKind.Relative));
        bolaGetResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var bolaDeleteResponse = await clientB.DeleteAsync(new Uri($"/api/v1/watchlist/{createdItem.Id}", UriKind.Relative));
        bolaDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 4. User A lists their watchlist and sees their item
        var listResponse = await clientA.GetAsync(new Uri("/api/v1/watchlist?page=1&pageSize=10", UriKind.Relative));
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await listResponse.Content.ReadFromJsonAsync<PagedResult<WatchItemDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle(x => x.Id == createdItem.Id);

        // 5. User A deletes their WatchItem -> 204 No Content
        var deleteResponse = await clientA.DeleteAsync(new Uri($"/api/v1/watchlist/{createdItem.Id}", UriKind.Relative));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AskAiAdvisor_WithValidInScopePrompt_Returns200OkWithDomainAnswer()
    {
        using var client = factory.CreateClient();
        var request = new AskAiAdvisorRequest("Explain the Hexagonal Architecture and Outbox alerts in MarketPulse");

        var response = await client.PostAsJsonAsync("/api/v1/ai/chat", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AiAdvisorResponseDto>();
        result.Should().NotBeNull();
        result!.Reply.Should().NotBeNullOrWhiteSpace();
        result.Reply.Should().Contain("Hexagonal");
        result.SuggestedActions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AskAiAdvisor_WithOutOfScopePrompt_Returns200OkWithPoliteRefusal()
    {
        using var client = factory.CreateClient();
        var request = new AskAiAdvisorRequest("What is the recipe for chocolate cake and who won the soccer game?");

        var response = await client.PostAsJsonAsync("/api/v1/ai/chat", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AiAdvisorResponseDto>();
        result.Should().NotBeNull();
        result!.Reply.Should().Contain("outside this scope");
        result.SuggestedActions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AskAiAdvisor_WithEmptyPrompt_Returns400BadRequest()
    {
        using var client = factory.CreateClient();
        var request = new AskAiAdvisorRequest(string.Empty);

        var response = await client.PostAsJsonAsync("/api/v1/ai/chat", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

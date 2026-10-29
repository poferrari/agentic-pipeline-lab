using System.Net;
using App.Infrastructure.RateLimiting;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace App.UnitTests.Infrastructure;

public sealed class OutboundLeakyBucketHandlerTests
{
    private sealed class StubInnerHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task SendAsync_WhenDisabledByConfiguration_ShouldPassThroughDirectly()
    {
        // Arrange
        var options = Options.Create(new LeakyBucketOptions
        {
            Enabled = false,
            Capacity = 5,
            LeakRatePerSecond = 10.0,
            MaxQueueWaitSeconds = 2
        });

        var innerHandler = new StubInnerHandler();
        var handler = new OutboundLeakyBucketHandler(options, NullLogger<OutboundLeakyBucketHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(handler);

        // Act
        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.frankfurter.app/latest"), CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        innerHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_WhenWithinCapacity_ShouldAllowRequestsToProceed()
    {
        // Arrange
        var options = Options.Create(new LeakyBucketOptions
        {
            Enabled = true,
            Capacity = 10,
            LeakRatePerSecond = 100.0,
            MaxQueueWaitSeconds = 5
        });

        var innerHandler = new StubInnerHandler();
        var handler = new OutboundLeakyBucketHandler(options, NullLogger<OutboundLeakyBucketHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(handler);

        // Act
        var response1 = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.frankfurter.app/latest"), CancellationToken.None);
        var response2 = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.frankfurter.app/latest"), CancellationToken.None);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        innerHandler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WhenQueueCapacityIsExceeded_ShouldThrowLeakyBucketOverflowException()
    {
        // Arrange
        // Capacity = 1, LeakRate = 0.001 (1 request every 1000 seconds, so no replenishment during test)
        var options = Options.Create(new LeakyBucketOptions
        {
            Enabled = true,
            Capacity = 1,
            LeakRatePerSecond = 0.001,
            MaxQueueWaitSeconds = 5
        });

        var innerHandler = new StubInnerHandler();
        var handler = new OutboundLeakyBucketHandler(options, NullLogger<OutboundLeakyBucketHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(handler);

        // Act & Assert
        // 1st request: consumes the single available token
        var task1 = invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.frankfurter.app/latest"), CancellationToken.None);
        await task1;

        // 2nd request: queues in the buffer (capacity = 1)
        var task2 = invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.frankfurter.app/latest"), CancellationToken.None);

        // 3rd request: exceeds queue limit -> immediate overflow rejection
        var act = async () => await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.frankfurter.app/latest"), CancellationToken.None);

        await act.Should().ThrowAsync<LeakyBucketOverflowException>()
            .WithMessage("*Outbound Leaky Bucket buffer is full*");
    }

    [Fact]
    public async Task SendAsync_WhenQueueWaitTimeoutExceeded_ShouldThrowLeakyBucketTimeoutException()
    {
        // Arrange
        // Capacity = 5, LeakRate = 0.05 (20 seconds to leak 1 token), MaxQueueWaitSeconds = 1
        var options = Options.Create(new LeakyBucketOptions
        {
            Enabled = true,
            Capacity = 5,
            LeakRatePerSecond = 0.05,
            MaxQueueWaitSeconds = 1
        });

        var innerHandler = new StubInnerHandler();
        var handler = new OutboundLeakyBucketHandler(options, NullLogger<OutboundLeakyBucketHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(handler);

        // 1st request consumes token immediately
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.frankfurter.app/latest"), CancellationToken.None);

        // 2nd request enters queue, but wait timeout (1s) is much shorter than replenishment (20s)
        var act = async () => await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.frankfurter.app/latest"), CancellationToken.None);

        await act.Should().ThrowAsync<LeakyBucketTimeoutException>()
            .WithMessage("*timed out*waiting in the Leaky Bucket buffer*");
    }
}


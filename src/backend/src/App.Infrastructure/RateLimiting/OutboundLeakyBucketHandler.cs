using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Infrastructure.RateLimiting;

/// <summary>
/// HTTP delegating handler implementing the Leaky Bucket traffic-shaping pattern.
/// Ensures that outbound calls to external dependencies (e.g., currency providers)
/// leak at a constant, steady rate regardless of upstream client burst intensity.
/// </summary>
public sealed class OutboundLeakyBucketHandler : DelegatingHandler
{
    private readonly RateLimiter? _limiter;
    private readonly TimeSpan _maxWaitTimeout;
    private readonly ILogger<OutboundLeakyBucketHandler> _logger;

    public OutboundLeakyBucketHandler(
        IOptions<LeakyBucketOptions> options,
        ILogger<OutboundLeakyBucketHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var opt = options.Value ?? new LeakyBucketOptions();
        if (opt.Enabled && opt.LeakRatePerSecond > 0 && opt.Capacity > 0)
        {
            // Leaky Bucket: replenish strictly 1 permit every (1000 / LeakRatePerSecond) milliseconds.
            // QueueLimit acts as the physical bucket capacity buffering bursts.
            var replenishmentIntervalMs = Math.Max(1, (int)Math.Round(1000.0 / opt.LeakRatePerSecond));

            _limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(replenishmentIntervalMs),
                QueueLimit = opt.Capacity,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });

            _maxWaitTimeout = TimeSpan.FromSeconds(Math.Max(1, opt.MaxQueueWaitSeconds));
            _logger.LogInformation(
                "Outbound Leaky Bucket initialized: Capacity={Capacity}, LeakRate={LeakRate} req/s ({IntervalMs}ms/req), MaxQueueWait={WaitSec}s",
                opt.Capacity,
                opt.LeakRatePerSecond,
                replenishmentIntervalMs,
                _maxWaitTimeout.TotalSeconds);
        }
        else
        {
            _maxWaitTimeout = TimeSpan.FromSeconds(5);
            _logger.LogInformation("Outbound Leaky Bucket is disabled by configuration.");
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_limiter is null)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(_maxWaitTimeout);

        RateLimitLease lease;
        try
        {
            lease = await _limiter.AcquireAsync(permitCount: 1, linkedCts.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Outbound request to {Uri} timed out after {Timeout}s waiting in the Leaky Bucket queue.",
                request.RequestUri,
                _maxWaitTimeout.TotalSeconds);

            throw new LeakyBucketTimeoutException(
                $"Outbound request timed out after {_maxWaitTimeout.TotalSeconds}s waiting in the Leaky Bucket buffer.",
                ex);
        }

        using (lease)
        {
            if (!lease.IsAcquired)
            {
                _logger.LogWarning(
                    "Outbound request to {Uri} was rejected because the Leaky Bucket queue capacity is full (overflow).",
                    request.RequestUri);

                throw new LeakyBucketOverflowException(
                    "Outbound Leaky Bucket buffer is full. Request dropped to protect external provider.");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _limiter?.Dispose();
        }

        base.Dispose(disposing);
    }
}

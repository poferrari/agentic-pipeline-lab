namespace App.Infrastructure.RateLimiting;

/// <summary>
/// Configuration options for the outbound Leaky Bucket rate limiter.
/// Controls constant outflow rate and buffer capacity for external API integrations.
/// </summary>
public sealed record LeakyBucketOptions
{
    public const string SectionName = "ExternalApis:FrankfurterLeakyBucket";

    /// <summary>
    /// Gets or sets whether the outbound leaky bucket rate limiter is active.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the maximum bucket capacity (queue size).
    /// Incoming burst requests beyond this limit overflow and are immediately rejected.
    /// </summary>
    public int Capacity { get; init; } = 20;

    /// <summary>
    /// Gets or sets the steady outflow leak rate in requests per second.
    /// Requests leak strictly at this constant speed (e.g., 5.0 = 1 request every 200ms).
    /// </summary>
    public double LeakRatePerSecond { get; init; } = 5.0;

    /// <summary>
    /// Gets or sets the maximum time in seconds a request may wait in the bucket queue
    /// before timing out to prevent thread starvation.
    /// </summary>
    public int MaxQueueWaitSeconds { get; init; } = 5;
}


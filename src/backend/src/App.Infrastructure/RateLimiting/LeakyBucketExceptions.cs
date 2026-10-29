namespace App.Infrastructure.RateLimiting;

/// <summary>
/// Thrown when the outbound leaky bucket queue is completely full and cannot accept additional burst requests.
/// </summary>
public sealed class LeakyBucketOverflowException : HttpRequestException
{
    public LeakyBucketOverflowException(string message)
        : base(message, null, System.Net.HttpStatusCode.TooManyRequests)
    {
    }
}

/// <summary>
/// Thrown when an outbound request times out while waiting in the leaky bucket queue to be dispatched.
/// </summary>
public sealed class LeakyBucketTimeoutException : HttpRequestException
{
    public LeakyBucketTimeoutException(string message)
        : base(message, null, System.Net.HttpStatusCode.RequestTimeout)
    {
    }
}


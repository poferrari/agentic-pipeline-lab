using App.Application.MarketPulse.DTOs;
using App.Domain.Common;

namespace App.Application.Abstractions;

public interface IMarketExchangeProvider
{
    Task<Result<ExchangeRatesQuoteDto>> GetLatestRatesAsync(
        string baseCurrency,
        IReadOnlyCollection<string>? symbols,
        CancellationToken ct = default);
}

public interface ICurrentUserProvider
{
    Guid GetCurrentUserId();
    bool IsAuthenticated { get; }
}

public interface IEventBusPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : class;
}

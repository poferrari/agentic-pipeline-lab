using App.Application.MarketPulse.Commands.CreateWatchItem;
using App.Application.MarketPulse.Commands.DeleteWatchItem;
using App.Application.MarketPulse.Queries.GetExchangeRates;
using App.Application.MarketPulse.Queries.GetWatchItemById;
using App.Application.MarketPulse.Queries.GetWatchItems;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace App.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<GetExchangeRatesQueryHandler>();
        services.AddScoped<CreateWatchItemCommandHandler>();
        services.AddScoped<GetWatchItemsQueryHandler>();
        services.AddScoped<GetWatchItemByIdQueryHandler>();
        services.AddScoped<DeleteWatchItemCommandHandler>();

        return services;
    }
}

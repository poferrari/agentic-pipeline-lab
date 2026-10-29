using App.Application.Abstractions;
using App.Domain.Common;
using App.Domain.MarketPulse.Repositories;
using App.Infrastructure.ExternalApis.Frankfurter;
using App.Infrastructure.Messaging;
using App.Infrastructure.Persistence;
using App.Infrastructure.Persistence.Repositories;
using App.Infrastructure.Security;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

        var databaseConnectionString = configuration.GetConnectionString("Database");
        if (!string.IsNullOrWhiteSpace(databaseConnectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(databaseConnectionString));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("MarketPulseInMemoryDb"));
        }

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ICurrencyWatchItemRepository, CurrencyWatchItemRepository>();

#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        var frankfurterBaseUrl = configuration["ExternalApis:FrankfurterBaseUrl"]
            ?? "https://api.frankfurter.app/";

        services.AddHttpClient<IMarketExchangeProvider, FrankfurterExchangeAdapter>(client =>
        {
            client.BaseAddress = new Uri(frankfurterBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "MarketPulseLab-HexagonalApi/1.0");
        });

        var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq");
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<CurrencyAlertConsumer>();

            if (!string.IsNullOrWhiteSpace(rabbitMqConnectionString))
            {
                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitMqConnectionString));
                    cfg.UseMessageRetry(r => r.Exponential(
                        3,
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(7),
                        TimeSpan.FromSeconds(2)));

                    cfg.ReceiveEndpoint("marketpulse.currency-alerts", e =>
                    {
                        e.ConfigureConsumer<CurrencyAlertConsumer>(context);
                        e.PrefetchCount = 16;
                    });

                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                bus.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        services.AddScoped<IEventBusPublisher, MassTransitEventBusPublisher>();

        return services;
    }
}

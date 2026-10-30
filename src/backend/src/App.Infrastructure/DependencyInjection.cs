using App.Application.Abstractions;
using App.Application.MarketPulse.Events;
using App.Domain.Common;
using App.Domain.MarketPulse.Repositories;
using App.Domain.MarketPulse.Ports;
using App.Infrastructure.Ai;
using App.Infrastructure.ExternalApis.Frankfurter;
using App.Infrastructure.Messaging;
using App.Infrastructure.Persistence;
using App.Infrastructure.Persistence.Repositories;
using App.Infrastructure.RateLimiting;
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
        ArgumentNullException.ThrowIfNull(services);
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

        services.AddHttpClient<IChatAdvisor, OpenAiChatAdvisorAdapter>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "MarketPulseLab-AiAdvisor/1.0");
        });

#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        var frankfurterBaseUrl = configuration["ExternalApis:FrankfurterBaseUrl"];


        services.Configure<LeakyBucketOptions>(
            configuration.GetSection(LeakyBucketOptions.SectionName));
        services.AddTransient<OutboundLeakyBucketHandler>();

        services.AddHttpClient<IMarketExchangeProvider, FrankfurterExchangeAdapter>(client =>
        {
            if (!string.IsNullOrWhiteSpace(frankfurterBaseUrl) && Uri.TryCreate(frankfurterBaseUrl, UriKind.Absolute, out var uri))
            {
                client.BaseAddress = uri;
            }
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "MarketPulseLab-HexagonalApi/1.0");
        })
        .AddHttpMessageHandler<OutboundLeakyBucketHandler>();

        var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq");
        var retryLimit = configuration.GetValue("Messaging:Retry:Limit", 10);
        var initialIntervalSeconds = configuration.GetValue("Messaging:Retry:InitialIntervalSeconds", 1);
        var maxIntervalSeconds = configuration.GetValue("Messaging:Retry:MaxIntervalSeconds", 30);
        var intervalDeltaSeconds = configuration.GetValue("Messaging:Retry:IntervalDeltaSeconds", 2);

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<CurrencyAlertConsumer>();
            bus.AddConsumer<CurrencyAlertFaultConsumer>();

            if (!string.IsNullOrWhiteSpace(databaseConnectionString))
            {
                bus.AddEntityFrameworkOutbox<AppDbContext>(o =>
                {
                    o.UsePostgres();
                    o.UseBusOutbox();
                    o.QueryDelay = TimeSpan.FromSeconds(1);
                    o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
                    o.QueryMessageLimit = 100;
                    o.QueryTimeout = TimeSpan.FromSeconds(30);
                });
            }

            if (!string.IsNullOrWhiteSpace(rabbitMqConnectionString))
            {
                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitMqConnectionString));

                    cfg.ReceiveEndpoint("marketpulse.currency-alerts", e =>
                    {
                        e.Bind<CurrencyAlertNotificationEvent>();
                        e.Bind<ICurrencyAlertConsumerContract>();

                        e.UseMessageRetry(r =>
                        {
                            r.Ignore<ArgumentException>();
                            r.Ignore<ArgumentNullException>();
                            r.Exponential(
                                retryLimit,
                                TimeSpan.FromSeconds(initialIntervalSeconds),
                                TimeSpan.FromSeconds(maxIntervalSeconds),
                                TimeSpan.FromSeconds(intervalDeltaSeconds));
                        });

                        if (!string.IsNullOrWhiteSpace(databaseConnectionString))
                        {
                            e.UseEntityFrameworkOutbox<AppDbContext>(context);
                        }

                        e.ConfigureConsumer<CurrencyAlertConsumer>(context);
                        if (configuration.GetValue("RabbitMq:UseQuorumQueues", false))
                        {
                            e.SetQuorumQueue();
                            e.SetQueueArgument("x-delivery-limit", 5);
                        }
                        e.ConcurrentMessageLimit = 8;
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

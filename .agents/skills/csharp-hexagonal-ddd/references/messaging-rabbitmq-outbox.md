# MassTransit, RabbitMQ, and Transactional Outbox

Reference for the `csharp-hexagonal-ddd` skill. Load only when the task touches this area.

## 7. MassTransit Messaging, RabbitMQ & Transactional Outbox

### 7.1. Enterprise RabbitMQ Architecture Guardrails (Gago.io & .NET Best Practices)
1. **Producer Isolation (Exchange-Only Publishing)**: Producers NEVER bind directly to queues. Producers publish exclusively to Exchanges. Consumers own their queues and bindings (Exchange-to-Exchange topology).
2. **Quorum Queues (Raft Consensus)**:
   - Classic mirrored queues (HA) were permanently removed in RabbitMQ 4.0+.
   - Always declare business queues as Quorum Queues (`e.SetQuorumQueue()`) with broker-level delivery limit (`e.SetQueueArgument("x-delivery-limit", 5)`) to prevent infinite poison message loops.
3. **QoS (Prefetch vs Concurrency Ratio)**:
   - Formula: `PrefetchCount = ConcurrentConsumers * (2 to 3)`.
   - Never use `Prefetch = 0` (causes OutOfMemory and requeue storms).
   - Pair `e.ConcurrentMessageLimit = 8` with `e.PrefetchCount = 16`.
4. **Thread-Safety Rule for AMQP Channels (`IModel` / `IChannel`)**:
   - AMQP channels are strictly **NOT thread-safe**. Never share a raw channel across threads or concurrent Tasks. Let MassTransit manage channel pooling.
5. **Selective Retry Filters (Transient vs Permanent Failures)**:
   - Always ignore non-transient exceptions (`ArgumentException`, `ArgumentNullException`, schema validation errors) from retry policies so poison messages route immediately to the Dead Letter Queue (`_error`).

### 7.2. Production Outbox & RabbitMQ Configuration
```csharp
services.AddMassTransit(x =>
{
    // Transactional Outbox (PostgreSQL 17)
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UsePostgres(); // Native FOR UPDATE SKIP LOCKED
        o.UseBusOutbox();
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30); // Prevents table bloat
        o.QueryMessageLimit = 100;
        o.QueryTimeout = TimeSpan.FromSeconds(30);
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(configuration.GetConnectionString("RabbitMq"));

        cfg.ReceiveEndpoint("marketpulse.currency-alerts", e =>
        {
            // Quorum Queue (Raft Consensus)
            e.SetQuorumQueue();
            e.SetQueueArgument("x-delivery-limit", 5);

            // Optimal QoS ratio (2:1)
            e.ConcurrentMessageLimit = 8;
            e.PrefetchCount = 16;

            // Smart retry: ignore permanent errors
            e.UseMessageRetry(r =>
            {
                r.Ignore<ArgumentException>();
                r.Ignore<ArgumentNullException>();
                r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
            });

            // Inbox Pattern for idempotency
            e.UseEntityFrameworkOutbox<AppDbContext>(context);

            e.ConfigureConsumer<CurrencyAlertConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});
```
- **Idempotent Consumers**: Messages may be redelivered during transient failures. Track `EventId` or use MassTransit's Inbox to guarantee exactly-once business processing.

### 7.3. Post-Outbox Distributed Resilience & Indeterminate States (Dennis Rojas Standard)
1. **The Shared DbContext Invariant**:
   - The Outbox and Inbox tables **MUST** share the same physical database and `DbContext` as the domain entities. Never place the Outbox table in a separate database instance, as doing so immediately resurrects the *dual-write problem* without distributed transactions.
2. **Explicit `Unknown` / `InAnalysis` State Modeling**:
   - Network timeouts (`504 Gateway Timeout`, `HttpRequestException`) on external mutating calls (e.g. banking APIs, payment gateways) must **NEVER** be blindly categorized as `Failed` or retried unconditionally.
   - Transition the aggregate into an `Unknown` or `PendingVerification` state. Suspend automatic retries and delegate resolution to a dedicated status-query worker or reconciliation background job.
3. **Continuous Reconciliation Jobs**:
   - Outbox only guarantees local dispatch; downstream consumers, network partitions, and Dead Letter Queues (DLQ) require scheduled reconciliation workers auditing unconfirmed transactions.

---

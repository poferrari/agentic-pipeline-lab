---
name: cqrs-and-event-sourcing
description: >-
  Actionable architecture and implementation recipes for Command Query Responsibility Segregation (CQRS)
  and Event Sourcing (EventStoreDB / PostgreSQL append-only streams, Optimistic Concurrency with Revision,
  Projections, Read Models, and Transactional Outbox). Based on Renato Augusto's Architect Blueprint and Martin Fowler standards.
---

# CQRS & Event Sourcing Architecture Guide (`cqrs-and-event-sourcing`)

This skill provides deterministic engineering patterns, decision trees, and code standards for applying **CQRS** and **Event Sourcing** in high-throughput, mission-critical distributed systems.

---

## 1. CQRS Decision Matrix (When to use vs When to avoid)

| Criteria | Traditional CRUD | Logical CQRS (L1) | Polyglot CQRS (L3) | Event Sourcing + CQRS |
| :--- | :--- | :--- | :--- | :--- |
| **Read-to-Write Ratio** | $\le 5:1$ | $5:1$ to $20:1$ | $> 50:1$ (Read-Heavy) | Any (Focus is History/Audit) |
| **Audit & Temporal State** | Basic `updated_at` | Audit tables | Audit tables | **Mandatory native audit log** |
| **Query Complexity** | Flat, simple tables | Light DTO mapping | Deep multi-table JOINs | Pre-calculated projections |
| **Consistency** | Strong (ACID) | Strong (ACID) | **Eventual Consistency** | **Eventual Consistency** |
| **Cognitive Cost** | Lowest | Low | Moderate | High |

---

## 2. Command vs. Query Architectural Boundary

```
[Inbound HTTP Request]
         |
         +---> If Mutation: Command -> CommandValidator -> CommandHandler -> Aggregate Root -> Outbox/DB -> Result<TId>
         |
         +---> If Retrieval: Query -> QueryValidator -> QueryHandler -> Direct SQL / Cache -> Result<TDto>
```

### 2.1. Command Rules
- Represents intent in imperative tense (`CreateOrderCommand`, `ShipOrderCommand`).
- Validated via FluentValidation before touching Domain entities.
- Must execute through the **Aggregate Root** to enforce domain invariants.
- Must NOT return query models or full aggregate state. Returns `Result<Guid>` or `Result.Success()`.

### 2.2. Query Rules
- Strictly **side-effect free** (Idempotent and safe).
- Bypasses Domain Entities, Aggregates, and EF Core change tracker completely.
- Reads directly from read-optimized views or tables using Dapper or compiled projections.
- Maps directly to flat, UI-ready DTOs without nested navigation properties.

---

## 3. Event Sourcing Implementation Blueprint

### 3.1. Core Aggregate Pattern with Event Stream
```csharp
public abstract class EventSourcedAggregateRoot
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();
    
    public Guid Id { get; protected set; }
    public long Version { get; protected set; } = -1; // Revision counter

    public IReadOnlyCollection<IDomainEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    // Replay historical events to reconstruct state
    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var @event in history)
        {
            ApplyEvent(@event, isNew: false);
        }
    }

    protected void RaiseEvent(IDomainEvent @event)
    {
        ApplyEvent(@event, isNew: true);
    }

    private void ApplyEvent(IDomainEvent @event, bool isNew)
    {
        When(@event);
        Version++;
        if (isNew)
        {
            _uncommittedEvents.Add(@event);
        }
    }

    // Pattern matching method implemented by concrete aggregates
    protected abstract void When(IDomainEvent @event);
}
```

### 3.2. Optimistic Concurrency with Stream Revision
```csharp
public async Task AppendEventsAsync(
    string streamId, 
    long expectedRevision, 
    IEnumerable<IDomainEvent> events, 
    CancellationToken ct)
{
    // If the stream was mutated by another process between Load and Append,
    // the store throws ConcurrencyException -> Application catches and retries
    await _eventStore.AppendToStreamAsync(
        streamId, 
        StreamRevision.FromInt64(expectedRevision), 
        events, 
        ct);
}
```

---

## 4. Projections & Polyglot Synchronization

### 4.1. Projection Worker Pipeline
1. **Catch-up Subscription:** Listens to `$all` or stream categories (e.g. `$ce-orders`).
2. **Idempotency Gate:** Checks if `event.EventNumber` has already been applied to the Read Model before updating.
3. **Optimized Persistence:**
   - **PostgreSQL Read Table:** Executes flat `INSERT ... ON CONFLICT DO UPDATE`.
   - **Redis Cache:** Sets JSON serialized view with TTL for lightning-fast mobile/web fetch.
   - **Elasticsearch:** Indexes full document for multi-attribute filtering.


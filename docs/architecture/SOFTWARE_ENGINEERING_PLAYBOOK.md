# Enterprise Software Engineering & Architecture Playbook

This playbook establishes the architectural foundations, design patterns, quality standards, and engineering workflows adopted by this autonomous agentic pipeline, synthesized from industry leaders (*Eric Evans, Martin Fowler, Robert C. Martin, Alex Xu / ByteByteGo, and Renato Augusto's Architect Roadmap*).

---

## 1. Architectural Career and Engineering Levels

| Level | Focus Area | Primary Artifacts | Key Deliverables |
| :--- | :--- | :--- | :--- |
| **Enterprise Architecture** | Business alignment, tech governance, capability roadmaps. | Tech radar, capability maps, build-vs-buy policies. | Cross-organization strategy, platform standards, vendor neutrality. |
| **Solution Architecture (HLD)** | High-Level Design, integration topologies, NFRs, scale. | C4 Context/Container diagrams, ADRs, capacity models. | System boundaries, synchronous/asynchronous flows, resilience topology. |
| **Software Architecture (LLD)** | Low-Level Design, tactical DDD, design patterns, clean code. | C4 Component/Code diagrams, domain models, APIs, tasks. | Aggregate invariants, Object Calisthenics, test pyramid, zero deadlocks. |

---

## 2. Strategic and Tactical Domain-Driven Design (DDD)

### 2.1. Strategic Design
- **Subdomain Partitioning**:
  - **Core Domain**: The system's primary value proposition and competitive edge. Requires highest architectural investment and custom engineering.
  - **Supporting Subdomain**: Necessary custom business capabilities supporting the core.
  - **Generic Subdomain**: Commodity functionalities with no competitive advantage. **Delegate to proven market solutions** (e.g. Supabase Auth for identity, CloudAMQP for RabbitMQ, PostgreSQL for relational storage).
- **Bounded Contexts**: Linguistic and conceptual boundaries where a specific domain model applies without contradiction.
- **Context Mapping Patterns**:
  - **Anti-Corruption Layer (ACL)**: Defensive translation boundary insulating the internal domain model from foreign upstream models.
  - **Open Host Service (OHS) / Published Language (PL)**: Standardized protocol (e.g. REST API + OpenAPI 3.1) enabling downstream clients to consume services cleanly.
  - **Customer / Supplier**: Upstream provider negotiated directly with downstream consumer.

### 2.2. Tactical Design Patterns
- **Aggregate Root**: Cluster of entities and value objects treated as a single transactional consistency unit. External callers only reference the Root.
- **Value Objects**: Immutable objects distinguished by their attributes, self-validating in constructor/factory, comparing by structural equality (`readonly record struct` in C#).
- **Domain Invariants**: Business rules that must evaluate to `true` at all times. Invariants are protected via private constructors and explicit domain mutation methods.
- **Domain Events**: Dispatched upon significant business state changes (`IDomainEvent`), queued in-memory, and persisted to outbox tables atomically.

---

## 3. Object Calisthenics (The 9 Golden Rules of Code Excellence)

Every line of C# / TypeScript produced in this pipeline adheres to the 9 rules of Object Calisthenics:

1. **One Level of Indentation per Method**: Methods must remain flat and readable. Extract inner loops and conditionals into dedicated private methods.
2. **Never Use the `else` Keyword**: Employ guard clauses and early returns (`if (condition) return Result.Failure(...);`). Use polymorphism / Strategy pattern for branching.
3. **Wrap All Primitives and Strings**: Eliminate Primitive Obsession. Wrap domain concepts (`Email`, `Money`, `CurrencyCode`, `Percentage`) into self-validating Value Objects.
4. **First-Class Collections**: Encapsulate collections into classes or dedicated Aggregate Roots. Expose collections as `IReadOnlyCollection<T>`.
5. **One Dot per Line (Law of Demeter)**: Only communicate with immediate collaborators. Never chain property access (`a.B.C.Do()`).
6. **Do Not Abbreviate**: Identifiers must be fully spelled out and reveal clear intent in technical English (EN-US).
7. **Keep Classes Small**: Maximum 100 lines per class (ideal < 50 lines) and < 20 lines per method.
8. **No More than Two Instance Variables per Class**: Decompose complex state into cohesive Value Objects.
9. **No Getters / Setters (Tell, Don't Ask)**: Do not extract state to decide externally. Command the object to execute its own invariants (`account.Withdraw(amount)` instead of `account.Balance -= amount`).

---

## 4. Distributed Systems Resilience Patterns

```
              ┌────────────────────────────────────────────────────────┐
              │             Distributed Resilience Pipeline            │
              └───────────────────────────┬────────────────────────────┘
                                          │
       ┌──────────────────┬───────────────┴───────────────┬──────────────────┐
       ▼                  ▼                               ▼                  ▼
[ Rate Limiting ]  [ Circuit Breaker ]           [ Retry + Jitter ]  [ Transactional Outbox ]
Leaky/Token Bucket Open/Half-Open/Closed          Exponential Backoff  Atomic Dual-Write DB+Broker
```

### 4.1. Retry with Exponential Backoff and Randomized Jitter
- When a transient error occurs (e.g. network hiccup, rate limit response), retry with exponential delay:
  $$\text{Delay}(n) = \min(\text{MaxDelay}, \text{InitialDelay} \times 2^n) \pm \text{RandomJitter}$$
- Adding **Jitter** prevents synchronization waves (Thundering Herd / Retry Storm) from crashing recovering services.

### 4.2. Circuit Breaker Pattern
- Three states:
  - **Closed**: Requests pass normally; failures increment consecutive error counter.
  - **Open**: Trips when failure threshold is reached (e.g. 5 failures within 30s). Inbound calls fail immediately with a deterministic fallback without hitting the downstream service.
  - **Half-Open**: Trial state after cooldown period (e.g. 15s). Allows a small sample of probe calls. If they succeed, resets to Closed; if they fail, trips back to Open.

### 4.3. Bulkhead and Request Timeout
- Every external I/O call enforces a strict, immutable timeout (e.g. 3 to 5 seconds).
- Isolate thread pools and concurrency quotas so that latency in one downstream integration cannot starve unrelated services.

### 4.4. Transactional Outbox Pattern
- Solves the dual-write problem across PostgreSQL and RabbitMQ:
  1. Business entity state mutation and outbox event are committed within the **same local database transaction**.
  2. MassTransit background delivery worker polls/streams staged outbox messages and dispatches to RabbitMQ.
  3. Guarantees *At-Least-Once* delivery with zero message loss under broker failures.

### 4.5. Consumer Idempotency
- Brokers may redeliver messages upon network disconnects.
- Consumers verify message `EventId` or apply natural idempotency checks in the database, ensuring duplicate deliveries execute with zero side-effects.

---

## 5. Performance Anti-Patterns Catalog

| Anti-Pattern | Manifestation | Root Cause | Architectural Mitigation |
| :--- | :--- | :--- | :--- |
| **Retry Storm** | Cascading downtime after a minor hiccup. | Retrying failed requests immediately in a loop. | Exponential Backoff with full Jitter + Circuit Breaker. |
| **N+1 Queries** | Hundreds of sequential SQL queries executing per HTTP request. | Accessing lazy-loaded navigation properties inside loops. | Projected `.Select()` queries, `.Include()` with `.AsSplitQuery()`. |
| **Chatty I/O** | High p95 latency despite low CPU usage. | Dozens of small sequential network calls across services. | Batch APIs, GraphQL / composite endpoints, L1 `HybridCache`. |
| **Busy Database** | Database CPU at 100% while application servers are idle. | Heavy computations and business logic in triggers/stored procedures. | Offload business logic to application layer; keep database focused on relational integrity. |
| **No Caching** | Hot relational queries repeated 1,000x per second. | Missing cache strategy for read-heavy reference data. | Multi-tier Cache-Aside (`HybridCache` L1 memory + L2 distributed) with short TTL. |
| **Noisy Neighbor** | One client monopolizing system throughput. | Unpartitioned concurrency and lack of rate limiting. | Partitioned Rate Limiting by client IP / authenticated User ID. |

---

## 6. Capacity Planning & System Design Estimation

Back-of-the-envelope estimation guidelines (ByteByteGo methodology):

1. **RPS & Concurrency**:
   - $\text{Average RPS} = (\text{Daily Active Users} \times \text{Requests/Day}) / 86400$
   - $\text{Peak RPS} = \text{Average RPS} \times 3$
2. **Memory & Cache Sizing**:
   - Apply the **80/20 Rule**: 20% of hot data generates 80% of read traffic. Size L1/L2 caches to hold that 20% working set.
3. **Database Connection Pool**:
   - Formula: $\text{Pool Size} = (\text{Core Count} \times 2) + \text{Disk Spindle Factor}$. For modern cloud SSDs, keep pool sizes lean (20 to 50 connections) behind a connection pooler (Supavisor / PgBouncer).
4. **Golden Signals Monitoring**:
   - Always monitor and alert on **Latency (p50/p95/p99)**, **Traffic (RPS)**, **Errors (5xx rate)**, and **Saturation (CPU/RAM/Pool)**.

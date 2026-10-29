---
name: software-architecture-patterns
description: >-
  Comprehensive guide for Software Architecture, System Design, Strategic and Tactical DDD,
  Object Calisthenics, Distributed Resilience Patterns (Circuit Breaker, Exponential Backoff + Jitter, Outbox, Idempotency),
  Performance Anti-Patterns, and Capacity Planning (ByteByteGo / Alex Xu / Renato Augusto Roadmap).
---

# Software Architecture Patterns & Engineering Principles (`software-architecture-patterns`)

This skill establishes advanced **System Design**, **Domain-Driven Design (DDD)**, **Object Calisthenics**, and **Distributed Systems Resilience** guidelines derived from industry standards (*Eric Evans, Martin Fowler, Robert C. Martin, Alex Xu / ByteByteGo, and Renato Augusto's Architect Roadmap*).

---

## 1. Architectural Levels & Roles

Every architectural decision operates across three distinct levels:

1. **Enterprise Architecture**:
   - Aligning organizational business capabilities with technological investments.
   - Long-term technology roadmaps, capability mapping, build-vs-buy governance.
2. **Solution Architecture (High-Level Design - HLD)**:
   - System boundaries, integration topology, data flow, synchronous vs asynchronous communication.
   - Cross-cutting concerns: Authentication, API Gateways, Service Meshes, Message Brokers (RabbitMQ/Kafka), and Cache topologies.
   - Non-Functional Requirements (NFRs), SLAs/SLOs, and disaster recovery.
3. **Software Architecture (Low-Level Design - LLD)**:
   - Internal modular structure, clean code practices, tactical design patterns (GoF).
   - Domain-Driven Design (DDD) tactical patterns, Object Calisthenics, interface segregation, and testability.

---

## 2. Domain-Driven Design (DDD) Deep Dive

### 2.1. Strategic Design
- **Subdomain Classification**:
  - **Core Domain**: The primary competitive advantage of the business (bespoke implementation, highest engineering investment).
  - **Supporting Subdomain**: Complements the core domain (custom built, but lower complexity).
  - **Generic Subdomain**: Commodity functionalities (delegate to SaaS/libraries: AuthN via Supabase, Billing via Stripe, Email via SendGrid).
- **Ubiquitous Language**: A shared, rigorous vocabulary between domain experts and software engineers, strictly reflected in code identifiers.
- **Bounded Contexts & Context Mapping**:
  - **Shared Kernel**: Two contexts share a subset of the domain model and database (use with extreme caution).
  - **Customer / Supplier**: Upstream context (Supplier) delivers contracts requested by Downstream context (Customer).
  - **Conformist**: Downstream conforms unconditionally to upstream model.
  - **Anti-Corruption Layer (ACL)**: Translating adapter insulating the internal domain model from external foreign models.
  - **Open Host Service (OHS) / Published Language (PL)**: Public, documented protocol/API (e.g. REST/OpenAPI, gRPC, Protobuf).

### 2.2. Tactical Design
- **Entity**: An object defined by its continuous identity across time and states, rather than its attributes.
- **Value Object**: Immutable concept defined solely by its attributes, self-validating in constructor, with structural equality (`readonly record struct` in C#).
- **Aggregate & Aggregate Root**: Consistency boundary encompassing one or more entities and value objects. All external references point strictly to the Aggregate Root. State changes execute exclusively through root methods maintaining domain invariants.
- **Domain Event**: Immutable record of an event that occurred within the domain (`OccurredOnUtc`), triggering side-effects within or across contexts.
- **Domain Service**: Pure domain logic that naturally does not belong to a single entity or value object.
- **Repository Interface**: Abstraction in the domain layer providing collection-like access to aggregate roots, implemented by infrastructure adapters.

---

## 3. Object Calisthenics (The 9 Golden Rules of Code Quality)

Apply these 9 rules to eliminate code smells and guarantee maintainability:

1. **Only One Level of Indentation per Method**:
   - Extract logic into small private methods. Eliminate nested loops and conditionals.
2. **Never Use the `else` Keyword**:
   - Use early returns, guard clauses, and polymorphism (Strategy pattern) instead of cascading `if/else`.
3. **Wrap All Primitives and Strings (Value Objects)**:
   - Avoid Primitive Obsession. Wrap `string email` into `Email`, `decimal amount` into `Money`, `Guid id` into typed identifiers.
4. **First-Class Collections**:
   - Any class containing a collection should contain no other member variables. Expose collections as `IReadOnlyCollection<T>`.
5. **One Dot per Line (Law of Demeter)**:
   - Do not talk to strangers: `a.GetB().GetC().DoSomething()` violates Demeter. Ask the immediate object to act: `a.Execute()`. (Exception: Fluent APIs, LINQ, and Builders).
6. **Do Not Abbreviate**:
   - Variable and method names must be expressive and clear. If a name is too long, the class/method likely violates the Single Responsibility Principle.
7. **Keep All Entities Small**:
   - Target classes under 100 lines (ideally < 50 lines) and methods under 20 lines.
8. **No Classes with More Than Two Instance Variables**:
   - Encourages high cohesion and composition. Group related fields into Value Objects.
9. **No Getters / Setters / Properties (Tell, Don't Ask)**:
   - Do not inspect an object's state to make decisions outside of it. Instruct the object to perform business operations internally: `account.Withdraw(amount)` instead of `if (account.Balance >= amount) account.Balance -= amount;`.

---

## 4. Distributed Systems Resilience Patterns

When services communicate across networks, failure is inevitable. Implement the following resilience patterns:

### 4.1. Retry with Exponential Backoff and Jitter
- Retrying immediately under failure creates a **Thundering Herd** (Retry Storm).
- Add exponential delay ($2^n \times \text{interval}$) plus randomized **Jitter** ($\pm \text{random}(0, \text{interval})$) to break synchronization among retrying clients.

### 4.2. Circuit Breaker
- Prevents cascading failures when a downstream dependency is saturated or down:
  - **Closed**: Normal execution; failures increment error counter.
  - **Open**: Error threshold reached; incoming calls fail immediately with fallback without executing network requests.
  - **Half-Open**: Trial period allowing limited probe calls to test dependency recovery.

### 4.3. Bulkhead & Timeout
- **Timeout**: Every network call must define an immutable timeout (e.g. 3-5 seconds). Unbounded calls lead to thread-pool starvation.
- **Bulkhead**: Partition thread pools and resource quotas so that failure in one dependency cannot exhaust capacity for other system operations.

### 4.4. Transactional Outbox Pattern
- Guarantees atomic dual-write between relational databases and message brokers:
  1. Business entity mutation and outbox event message are written to PostgreSQL inside the **same database transaction**.
  2. Background delivery worker (MassTransit BusOutbox or polling worker) reads staged messages and dispatches to RabbitMQ.
  3. Guarantees *At-Least-Once* delivery even if the message broker is temporarily offline.

### 4.5. Consumer Idempotency
- Because message brokers guarantee *At-Least-Once* delivery, duplicate deliveries can happen.
- Consumers must be idempotent: track processed `EventId` or use natural idempotency keys in PostgreSQL/Redis before executing business mutations.

---

## 5. Performance Anti-Patterns to Detect & Eradicate

| Anti-Pattern | Description | Recommended Solution |
| :--- | :--- | :--- |
| **Retry Storm** | Retrying failed requests without backoff/jitter, causing permanent self-inflicted DDoS. | Exponential backoff + full jitter + Circuit Breaker. |
| **N+1 Query Problem** | Querying a parent collection and issuing 1 secondary query per item inside a loop. | Use `.Include()` with `.AsSplitQuery()` or projected `.Select()`. |
| **Chatty I/O** | Making dozens of small, sequential network calls to retrieve related data. | Batch requests, composite endpoints, or local L1 caching (`HybridCache`). |
| **Busy Database** | Offloading heavy business computations, loops, or text processing to SQL stored procedures. | Keep database focused on relational persistence and indexing; execute business logic in application code. |
| **Noisy Neighbor** | One tenant exhausting shared memory, connection pools, or rate limits. | Partitioned rate limiting (by IP / Tenant / User ID) and connection pool caps. |
| **No Caching** | Re-executing identical, expensive read queries against PostgreSQL on hot data paths. | Multi-tier Cache-Aside (`HybridCache` L1 memory + L2 distributed) with explicit TTLs. |

---

## 6. Capacity Planning & Non-Functional Requirements (NFRs)

Apply **Back-of-the-Envelope Estimations** (ByteByteGo methodology):

1. **RPS & Concurrency**:
   - $\text{Daily Active Users (DAU)} \times \text{Requests/Day} / 86400 = \text{Average RPS}$.
   - $\text{Peak RPS} = \text{Average RPS} \times 2 \text{ to } 5$.
2. **Storage Estimation**:
   - $\text{Average Record Size (bytes)} \times \text{Daily Ingestion} \times \text{Retention Days} \times 1.3 \text{ (indexes/overhead)}$.
3. **Bandwidth Estimation**:
   - $\text{Peak RPS} \times \text{Average Payload Size (KB)} = \text{Outbound Bandwidth (MB/s)}$.
4. **Latency vs Throughput**:
   - High throughput does not guarantee low latency. Always measure percentiles: **p50 (median)**, **p95 (standard SLA)**, and **p99 (tail latency under burst)**.

---

## 8. Enterprise Messaging Topologies & RabbitMQ Architecture (Gago.io & AMQP Patterns)

1. **Exchange Topologies**:
   - **Direct**: Strict point-to-point command routing via exact `routing_key`.
   - **Fanout**: Broadcast pub/sub (1:N) with zero routing key evaluation cost (maximum throughput and lowest latency broker-side).
   - **Topic**: Hierarchical topic-based routing with `*` (single word) and `#` (zero or more words).
2. **Producer-Consumer Boundary Isolation**:
   - Producers only publish to Exchanges. Never bind producer code to queues.
   - Consumers declare and own their private Queues and Exchange-to-Exchange (E2E) bindings.
3. **Quorum Queues vs Classic Queues**:
   - Classic mirrored queues (HA) were permanently removed in RabbitMQ 4.0+.
   - Modern mission-critical systems must mandate **Quorum Queues** backed by Raft consensus for strong consistency (CP), partition tolerance, and automatic `x-delivery-limit` poison message protection.
4. **QoS (Quality of Service) Sizing**:
   - Golden rule: `PrefetchCount = ConcurrentConsumers * (2 to 3)`.
   - Prevents memory exhaustion from unbounded prefetch while avoiding TCP ping-pong latency.

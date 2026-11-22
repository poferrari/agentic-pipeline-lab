# CQRS, Messaging Topologies, and REST Contracts

Reference for the `software-architecture-patterns` skill. Load only when the task touches this area.

## 7. CQRS & Event Sourcing Architecture Blueprint (Renato Augusto & Martin Fowler Standards)

### 7.1. Command Query Responsibility Segregation (CQRS)
- **Separation of Concerns**:
  - **Commands**: Encapsulate intent, alter system state, execute business validation in Aggregate Roots, return lightweight IDs or `Result<T>`. Never return large querying projections.
  - **Queries**: Side-effect free, bypass heavy Domain models, read directly from optimized projections (DTOs) via Dapper or specialized storage.
- **Gradual Adoption Levels**:
  - **Level 1 (Logical CQRS)**: Single database, distinct Command/Query classes. Recommended default for clean separation.
  - **Level 2 (Separate Read Schemas)**: Write model in normalized 3FN tables; Read model in denormalized flat tables/views updated via Transactional Outbox.
  - **Level 3 (Polyglot Persistence)**: Write model in PostgreSQL (ACID); Read models projected into Redis (sub-millisecond key-value lookups) or Elasticsearch (full-text search and faceted filtering).
- **Architectural Safeguard**: Read Replicas are NOT CQRS. Read Replicas still replicate the identical relational schema and waste CPU executing complex multi-table JOINs. True CQRS shapes the read model to match user screen queries without JOINs.

### 7.2. Event Sourcing Pattern
- **Immutable Ledger as Source of Truth**: State is not saved as a mutable row (CRUD), but as an append-only stream of Domain Events.
- **Core Primitives**:
  - **Stream ID**: Maps 1:1 to Aggregate Root ID (`order-123`).
  - **Revision (Version)**: Monotonically increasing integer enabling Optimistic Concurrency Control (`ExpectedRevision`). Rejects concurrent writes with concurrency exceptions, preventing race conditions without pessimistic DB locks.
  - **Event Replay & Snapshots**: State is rebuilt in-memory by replaying historical events. Snapshots (taken every N events) bound replay latency for long-lived aggregates.
  - **Projections**: Background workers subscribe to event streams and populate Read Models asynchronously (Eventual Consistency).

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

---

## 9. RESTful API Design & Contract Engineering (Augusto Galego Blueprint)

1. **Resource-Oriented Modeling (Nouns over Verbs)**:
   - Use plural nouns for resource collections (`/api/v1/orders`, `/api/v1/users`).
   - Never embed RPC actions or verbs in URLs (e.g. forbid `/api/createOrder` or `/api/getUserById`). Use HTTP verbs (`POST`, `GET`, `PUT`, `PATCH`, `DELETE`).
   - Sub-resources limited to 2 nesting levels (`/api/v1/users/{userId}/orders`). Flatten deeper relationships (`/api/v1/order-items?orderId=123`).
2. **Network Resilience & Idempotency Key**:
   - All mutating non-idempotent endpoints (`POST`) executing financial, payment, or stateful side-effects must mandate the `Idempotency-Key: <uuid>` header.
   - Server caches the initial HTTP response in Redis (with TTL, e.g. 24h). Automatic retries on network drops return the cached payload without re-executing transactions.
3. **Semantic HTTP Status Codes**:
   - Never return HTTP `200 OK` with an error message in the payload body.
   - Success: `200 OK` (read/update), `201 Created` with `Location` header (creation), `204 No Content` (deletion).
   - Client Errors: `400 Bad Request` (syntactic validation), `401 Unauthorized` (unauthenticated), `403 Forbidden` (insufficient role/permissions), `404 Not Found`, `409 Conflict` (business rule / concurrency collision), `422 Unprocessable Entity` (semantic validation), `429 Too Many Requests` (with `Retry-After`).
4. **RFC 7807 Problem Details Standardization**:
   - All error responses must return `Content-Type: application/problem+json` containing `type`, `title`, `status`, `detail`, `instance`, and structured `invalidParams`.
5. **Keyset / Cursor-Based Pagination**:
   - Prefer cursor pagination (`?cursor=...&limit=50`) over offset pagination (`limit/offset`) for large datasets and feeds to guarantee $O(1)$ indexing speed and prevent data skipping.

---

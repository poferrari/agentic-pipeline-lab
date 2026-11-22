# Performance Anti-Patterns and Capacity Planning

Reference for the `software-architecture-patterns` skill. Load only when the task touches this area.

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

## Concurrency Control in Balances: Ledger vs Materialized Position

- **Symptom**: Blind in-memory calculation (`balance = balance - amount`) followed by an unqualified `UPDATE` causes lost updates (two 80-unit debits approved on a 100-unit balance). Summing ledger history on every request does not close the race window and degrades as history grows.
- **Rule**: Enforce atomic conditional updates in the database with an invariant guard: `UPDATE account_position SET available = available - :amount, version = version + 1 WHERE id = :id AND available >= :amount`. Check rows affected; if 0, reject immediately. Never call remote services while holding row locks. Treat external timeouts as INCONCLUSIVE and never release holds without reconciliation.

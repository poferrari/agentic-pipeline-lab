# ADR-001: Hexagonal Architecture with Frankfurter Public API Integration, HybridCache, and Resilient Container Deployment on Render

- **Status**: `Accepted`
- **Date**: `2026-10-28`
- **Decision Makers**: `domain-architect-planner`

---

## 1. Context and Problem Statement
We need to design, build, and deploy **MarketPulse Lab** to the target repository `https://github.com/poferrari/agentic-pipeline-lab` with containerized deployment on **Render** (`prj-d6rakhshg0os73a5ahqg` / `evm-d6rakhshg0os73a5ahr0`). The system must integrate a real public financial API without paid keys, enforce strict Hexagonal Architecture and Domain-Driven Design (DDD), implement two-tier caching and asynchronous messaging, and provide 100% automated test coverage while maintaining a zero-dollar MVP operational cost.

## 2. Considered Alternatives
1. **Option A (Selected): Modular Hexagonal Monolith in .NET 9 Minimal API with Frankfurter Public API + HybridCache + Adaptive MassTransit/EF Core**
   - *Pros*: Zero external subscription cost (ECB official currency rates), in-memory L1 cache with stampede protection, single container deployable on Render Free Tier with graceful in-memory fallback, and production-ready PostgreSQL/RabbitMQ support via environment variables.
   - *Cons*: Frankfurter updates rates once daily (working days) according to ECB schedule (sufficient for MVP watchlist tracking).
2. **Option B: Distributed Microservices with Paid Currency APIs and Mandatory External Message Brokers**
   - *Rejected*: High fixed infrastructure cost, network latency between microservices, unnecessary cognitive and operational complexity for MVP validation.

## 3. Decision
1. **Decoupled Public API Integration**: Use the official **Frankfurter API (`https://api.frankfurter.app`)** (European Central Bank currency rates) encapsulated behind a driven adapter (`FrankfurterExchangeAdapter`) implementing the output port `IMarketExchangeProvider`, combined with `HybridCache` and exponential backoff retry.
2. **Adaptive Infrastructure for Cloud Containers**: Enable PostgreSQL (EF Core) + RabbitMQ (MassTransit) when connection strings are present, while providing an automatic in-memory fallback for standalone Render Free Tier container execution and automated `WebApplicationFactory` integration tests.
3. **Hardened Containerization**: Multi-stage `Dockerfile` on .NET 9 Alpine exposing non-privileged port `8080`, running under a non-root user (`$APP_UID`), with active `/health/live` and `/health/ready` probes.

## 4. Consequences and Impacts
- **Positive Impact**: Clean domain isolation, zero API key dependencies, instant continuous integration passing 100% of tests, and one-click Render deployment.
- **Trade-offs**: When scaling to multi-instance high throughput, an external Redis/PostgreSQL and RabbitMQ broker should be provisioned.

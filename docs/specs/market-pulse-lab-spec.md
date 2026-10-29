# Technical and Functional Specification: `MarketPulse Lab (Intelligent Currency & Market Watchlist Hub)`

> **Status**: `READY_FOR_DEV`  
> **Date**: `2026-10-28`  
> **Authors**: `product-requirements-analyst` & `domain-architect-planner`  
> **Target Repository**: `https://github.com/poferrari/agentic-pipeline-lab`  
> **Cloud Environment**: `Render Containers (prj-d6rakhshg0os73a5ahqg / evm-d6rakhshg0os73a5ahr0)`  

---

## 1. Context, Problem, and Business Objective (MVP)
- **Problem**: Investors, importers, digital nomads, and financial operators need to monitor official foreign exchange rates (e.g., `USD/BRL`, `EUR/BRL`, `EUR/USD`, `GBP/BRL`) in real time based on the European Central Bank (ECB) and register customized target rate alerts (`TargetRate`) with automated spread calculation and asynchronous event emission, without paying expensive proprietary financial terminal licenses.
- **MVP Objective**: Deliver a production-grade **REST API + Hexagonal Modular Monolith in C# (.NET 9 Minimal API)** that:
  1. Consumes the official **Frankfurter Exchange Rates API (`https://api.frankfurter.app`)** via a resilient driven adapter (`IMarketExchangeProvider` -> `FrankfurterExchangeAdapter`) with `HttpClient`, timeouts, exponential retries, and two-tier caching (`HybridCache` L1/L2).
  2. Manages currency alerts and watchlists (**`CurrencyWatchItem` Aggregate Root**) scoped strictly by authenticated user (preventing BOLA/IDOR via `OwnerId`), enriched with live market rates, percentage spread (`SpreadPercentage`), and trigger status (`IsTriggered`).
  3. Raises **Domain Events** (`WatchItemCreatedDomainEvent`, `TargetRateTriggeredDomainEvent`) and publishes **Integration Events** (`WatchItemCreatedIntegrationEvent`) via **MassTransit + RabbitMQ** (with automatic in-memory fallback for standalone single-instance containers on Render Free Tier).
  4. Exposes OpenAPI v1 contracts (`/api/v1/rates/{baseCurrency}`, `/api/v1/watchlist`, `/health/live`, `/health/ready`) with `TypedResults`, `FluentValidation`, `ProblemDetails (RFC 7807)`, Rate Limiting, and OWASP Security Headers.
- **Out of Scope (Non-Goals / YAGNI)**:
  - Real execution of currency trade orders or banking integration (PIX/SWIFT).
  - High-frequency cryptocurrency WebSockets (focus is strictly on ECB-supported ISO-4217 fiat currencies).
  - Stripe subscription billing during the MVP phase.

---

## 2. Open Questions, Assumptions, and Architectural Decisions

| ID | Topic / Question | Status | Decision / Agreed Solution |
| :--- | :--- | :--- | :--- |
| `OQ-01` | Which external public API will provide market rates without key quota exhaustion? | `RESOLVED` | **Frankfurter Public API (`https://api.frankfurter.app/latest`)**, open-source, no API key required, powered by ECB daily reference rates. |
| `OQ-02` | How does the system handle temporary network degradation or downtime from the external API? | `RESOLVED` | `FrankfurterExchangeAdapter` implements `HybridCache` (L1 5 min / L2 15 min TTL), a 5s timeout, and deterministic fallback cache to prevent cascading failures. |
| `OQ-03` | How are authentication and BOLA/IDOR handled in testing and demo environments? | `RESOLVED` | Full support for **Supabase Auth JWT Bearer** (`sub` claim) alongside a configurable demo/test header (`Auth:AllowDemoHeader = true` reading `X-Demo-User-Id`), enforcing strict `401 Unauthorized` for anonymous requests and `404 Not Found` across different `OwnerId`s. |
| `OQ-04` | How to operate database and messaging in a single-instance Render container without external services provisioned? | `RESOLVED` | Documented in `ADR-001`: The `Infrastructure` layer supports PostgreSQL (`Npgsql`) + RabbitMQ (`MassTransit`) by default via ConnectionStrings, with automatic in-memory fallback for standalone Render Free Tier mode and `WebApplicationFactory` tests. |
| `OQ-05` | Which ISO-4217 currencies are supported in the domain? | `RESOLVED` | Standard 3-letter alphabetic codes (`USD`, `EUR`, `BRL`, `GBP`, `JPY`, `CAD`, `AUD`, `CHF`, `CNY`, `MXN`). `BaseCurrency` and `TargetCurrency` cannot be identical. |

---

## 3. Functional Requirements (FR) and Acceptance Criteria (BDD)

### `FR-001`: Real-Time Currency Rates Query (`GET /api/v1/rates/{baseCurrency}`)
- **Description**: Returns the latest exchange rates for an ISO-4217 base currency, optionally filtered by target currencies (`symbols`), using `HybridCache` and the Frankfurter public adapter.
- **Authorization**: `Public` (`.AllowAnonymous()`, protected by Rate Limiting).
- **BDD Scenarios**:
  - **Scenario 1 (Valid Base Currency)**:
    - **Given** a client requests `GET /api/v1/rates/USD?symbols=BRL,EUR`
    - **When** processed by `GetExchangeRatesQueryHandler`
    - **Then** returns `200 OK` with `baseCurrency: "USD"`, `referenceDate`, `provider`, and a dictionary of rates > 0 for `BRL` and `EUR`.
  - **Scenario 2 (Invalid Base Currency)**:
    - **Given** a client requests `GET /api/v1/rates/INVALID123`
    - **When** validated by `GetExchangeRatesQueryValidator`
    - **Then** returns `400 Bad Request` in RFC 7807 `ProblemDetails` with code `Currency.InvalidCode`.

### `FR-002`: Create Currency Watchlist Item (`POST /api/v1/watchlist`)
- **Description**: Authenticated users can register a currency pair (`BaseCurrency` -> `TargetCurrency`), target rate (`TargetRate > 0`), trigger direction (`NotifyWhenAbove`), and optional notes. The system looks up the live rate, checks if the target is already met, raises domain events, and publishes an integration event.
- **Authorization**: `Authenticated User` (`.RequireAuthorization()`).
- **BDD Scenarios**:
  - **Scenario 1 (Happy Path)**:
    - **Given** authenticated user `OwnerId = u1` posts `{ "baseCurrency": "USD", "targetCurrency": "BRL", "targetRate": 5.50, "notifyWhenAbove": true }`
    - **When** no duplicate active pair exists for this `OwnerId`
    - **Then** persists `CurrencyWatchItem`, raises `WatchItemCreatedDomainEvent`, publishes `WatchItemCreatedIntegrationEvent`, and returns `201 Created` with `Location` header.
  - **Scenario 2 (Duplicate Pair Conflict)**:
    - **Given** an existing active pair `USD` -> `BRL` for this user
    - **When** the user attempts to register the same pair again
    - **Then** returns `409 Conflict` (`ProblemDetails`) with code `WatchItem.DuplicatePair`.

### `FR-003`: Enriched Paginated User Watchlist (`GET /api/v1/watchlist`)
- **Description**: Paginated listing of watchlist items belonging strictly to the authenticated `OwnerId`, enriched with live rates, percentage spread, and `IsTriggered` flag.

### `FR-004`: Watchlist Item Lookup and Deletion with BOLA/IDOR Guard (`GET` and `DELETE /api/v1/watchlist/{id}`)
- **Description**: Allows retrieval or removal of a specific item by `id`. If another user attempts access, returns `404 Not Found`.

---

## 4. Non-Functional Requirements (NFR)
- **Performance**: `p95 < 150ms` for cached queries via `HybridCache`.
- **Stateless API**: Ready for Web and Mobile clients via OpenAPI v1.
- **OWASP Security**: Strict BOLA/IDOR prevention by `OwnerId`, immutable DTOs, pagination caps (`MaxPageSize = 100`), rate limiting (`60 req/min`), and security headers.

---

## 5. Domain Modeling (DDD) & Hexagonal Architecture
- **Context**: `MarketPulse`
- **Aggregate Root**: `CurrencyWatchItem`
- **Value Objects**: `CurrencyCode` (`readonly record struct`), `TargetRate` (`readonly record struct`)
- **Domain Events**: `WatchItemCreatedDomainEvent`, `TargetRateTriggeredDomainEvent`
- **Output Ports**: `ICurrencyWatchItemRepository`, `IUnitOfWork`, `IMarketExchangeProvider`, `ICurrentUserProvider`, `IEventBusPublisher`

---

## 6. Definition of Ready (DoR) Certification
- [x] All criteria from `docs/pipeline/DEFINITION_OF_READY.md` verified and approved (`100% READY_FOR_DEV`).

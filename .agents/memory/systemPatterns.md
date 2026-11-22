# System Architecture and Codebase Patterns

This document is the living single source of truth for architectural conventions and engineering standards established in this repository. All autonomous agents must consult and adhere to these patterns.

---

## 1. Backend Architecture (.NET 8/9+ C#)

### 1.1. Structural Style: Hexagonal Monolith (Ports & Adapters) with DDD
- **Strict Layer Isolation**:
  - `Domain`: Pure business rules, Aggregate Roots, Entities, Value Objects (`readonly record struct` or `record`), Domain Events, Domain Errors. Zero external dependencies.
  - `Application`: Use cases, CQRS Commands/Queries, Handlers, Input Validators (`FluentValidation`), Output Port Interfaces.
  - `Infrastructure`: Driven adapters (EF Core PostgreSQL, MassTransit RabbitMQ Outbox, Supabase JWT handler, HybridCache).
  - `Api`: Driving adapter (ASP.NET Core Minimal APIs, Middlewares, HealthChecks).

### 1.2. Error Handling: Result Pattern
- **Never throw exceptions for business validation or domain rule violations**.
- Use explicit `Result<T>` or `Result` with strongly-typed `Error` records.
- In Minimal APIs, map domain errors directly into RFC 7807 `ProblemDetails` via `TypedResults`.

### 1.3. Minimal API Routing & Organization
- Group endpoints logically using the `IEndpoint` interface pattern with `MapGroup` and version prefixes (`/api/v1/...`).
- Type all responses with `Results<Ok<TResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>` for compile-time OpenAPI schema generation.

### 1.4. Security & Multi-tenancy
- Delegate Authentication to **Supabase Auth** (asymmetric JWKS JWT validation).
- Never implement custom password hash or raw OAuth tables.
- Isolate tenant data via Postgres Row-Level Security (RLS) or application-level tenant filters.

---

## 2. Frontend Architecture (React / Next.js + TypeScript)

### 2.1. Framework & Type Safety
- **Strict TypeScript** (`"strict": true`, no `any`, no unsafe casts).
- Generate API contracts and types directly from the backend `openapi.json` (`openapi-typescript`).

### 2.2. Async Server State Management
- Use **TanStack Query v5** (`useQuery`, `useMutation`).
- Maintain query keys as deterministic tuples (e.g. `['users', 'detail', id]`).
- Do not duplicate server state into local React component state (`useState`).

### 2.3. Form Handling & Validation
- Standard: **React Hook Form** paired with **Zod** resolvers.
- Mirror backend FluentValidation validation rules in Zod schemas.

---

## 3. Uncle Bob Disciplines (`ub-mode`) & Quality Gates
- **CRAP Index**: Keep every method under $\text{CRAP} < 10$.
- **Dryer Ratio**: Structural duplication strictly under $0.8$.
- **Anti-Crash Guard**: Prohibit superficial null-checks that mask invalid state origins; make illegal states unrepresentable.
- **Mutation Testing**: Ensure tests assert behavioral outcomes, not trivial mock invocations.


---

## 4. Quality Gate Tooling (machine-verified, not prose)
- **Ground truth**: `scripts/validate-all.ps1` ends each step as PASS, FAIL, or NOT RUN. Exit 0 = fully green, 1 = failed, 2 = incomplete. Only exit 0 writes `.agents/.state/validate-all.ok`, which the Stop hook compares against the workspace fingerprint.
- **CRAP < 10**: `scripts/quality/crap_report.py` over OpenCover reports with a ratchet baseline (`crap-baseline.json`).
- **Duplication**: `jscpd` via `.jscpd.json` (percentage threshold; a practical proxy for the Dryer rule).
- **Mutation**: Stryker.NET via `dotnet-tools.json` and `src/backend/stryker-config.json` (`-Mutation` switch, weekly workflow).
- **EN-US rule**: `scripts/quality/check_english_only.py` with justified exceptions in `english-allowlist.txt`.
- **Safety hooks**: single policy in `.agents/scripts/safety-policy.ps1` shared by the Claude and Gemini hooks; behavioral tests in `.agents/scripts/tests/`.

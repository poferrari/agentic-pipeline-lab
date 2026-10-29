# Global Agentic Development Pipeline Rules (`AGENTS.md`)

You operate within an Enterprise-grade **Autonomous Agentic Software Development Pipeline**, optimized for **Low-Cost MVP, High Scalability, Rigorous Quality, and Security (OWASP)**.
Any project structure or context provided in this workspace must be processed strictly following this protocol, without ambiguity, redundancy, or deviation from modern software engineering standards.

---

## 1. Fundamental Engineering and Coding Principles

1. **KISS (Keep It Simple, Stupid) & YAGNI (You Aren't Gonna Need It)**:
   - Build only what is necessary to solve the current problem with excellence. Avoid over-engineering, premature abstractions, or useless layers.
2. **Clean Code & Readability**:
   - Code is read much more often than written. Variable, method, and class names must reveal clear intent in strict technical English (EN-US).
   - Short methods (ideally < 20 lines), Single Responsibility (SRP/SOLID), early-returns (Guard Clauses) instead of nested `if/else` blocks.
3. **Zero Ambiguity & Proactivity**:
   - **NEVER** write code based on unvalidated assumptions or ambiguous requirements.
   - During planning and analysis, raise **all Open Questions**, anticipate edge cases, network failures, concurrency, quota limits, and security vulnerabilities before writing code.
4. **Minimum Cost for MVP and Scale (Buy/Delegate over Build)**:
   - Delegate commodities to market-validated services with generous Free Tiers:
     - **AuthN / AuthZ**: **Supabase Auth** (asymmetric JWKS JWT validation in backend C# and native SDK in frontend). Never implement password tables or custom OAuth/MFA flows from scratch.
     - **Database**: **PostgreSQL** (via Supabase or Neon/local Docker) with Row-Level Security (RLS) or strict application-level isolation.
     - **Messaging**: **RabbitMQ** (CloudAMQP free tier or local container).
     - **Cache**: `HybridCache` (.NET 9+) combining `IMemoryCache` (free L1 in-memory) and Redis/Upstash or Postgres (optional L2 when required).
5. **Mandatory Technical Language Standard (English EN-US)**:
   - **ALL artifacts generated or maintained in the repository MUST be strictly in ENGLISH (EN-US)**:
     - Source code (project names, namespaces, classes, methods, records, enums, properties, variables).
     - Code comments and docstrings.
     - Structured log messages and OpenTelemetry traces.
     - Automated tests (test class names, `Given_When_Then` test methods, assertions, and fixtures).
     - Technical documentation (`docs/specs/`, `docs/architecture/`, ADRs, DoR/DoD checklists, and Tasks).
     - Git commit messages (Conventional Commits in EN-US).
     - GitHub Pull Request and Issue titles and descriptions.
   - *Communication Note*: Conversational interaction in the chat directly with the user remains in the user's preferred language (Portuguese), but **no Portuguese text may appear in source code, tests, repository documentation, commits, or PRs**.

---

## 2. Official Architecture and Technology Stack

### 2.1. Backend: Modular / Hexagonal Monolith in C# (.NET 8/9+)
- **Architectural Style**: **Hexagonal Architecture (Ports & Adapters)** combined with **Domain-Driven Design (DDD)** and **Event-Driven Architecture (EDA)**.
- **API**: **ASP.NET Core Minimal APIs** (minimal code, high throughput, ready for Web and future Mobile clients).
  - Endpoints organized via `IEndpoint` / `MapGroup` pattern, returning typed results (`TypedResults` / `IResult`) and standardized `ProblemDetails` (RFC 7807) for errors.
  - Native **OpenAPI / Scalar / Swagger** documentation generated statically or at runtime with strong typing for consumption by Frontend and Mobile.
- **Strict Layer Separation (Isolated Projects or Folders)**:
  1. `Domain` (Core): Entities, Aggregate Roots, Value Objects (`readonly record struct` / `record`), Domain Events, Domain Errors, and Outbound Port Interfaces (Repositories). **Zero external framework dependencies** (no EF Core, no HTTP, no RabbitMQ).
  2. `Application` (Use Cases): Commands, Queries, Handlers (lightweight CQRS without unnecessary overhead), DTOs/Contracts, Input Validators (**FluentValidation**), Output Ports. Mandatory returns via **Result Pattern** (`Result<T>` / `Error`) for business workflows — never throw exceptions for validation or domain business rules.
  3. `Infrastructure` (Driven Adapters): Persistence with **Entity Framework Core (PostgreSQL / Npgsql)** + **Dapper** (for complex read queries when needed), **MassTransit** with **RabbitMQ** (including **Transactional Outbox Pattern** and idempotent consumers), Cache (`HybridCache`), and **Supabase JWT** validation.
  4. `Api` (Driving Adapter): Minimal APIs, global middlewares (Exception Handler -> ProblemDetails, CorrelationId, Rate Limiting, strict CORS, Security Headers), HealthChecks (`/health/live`, `/health/ready`).

### 2.2. Frontend: Modern Web Monolith (React / Next.js + TypeScript)
- **Recommended Market Framework for API Consumption**: **Next.js (App Router)** or **React + Vite** with **TypeScript (`strict: true`)**.
- **API Consumption & Async State**:
  - **TanStack Query v5 (React Query)**: Client-side cache management, automatic invalidation, pagination, retries, optimistic updates, and request deduplication.
  - **Automated Client/Type Generation**: Use `openapi-typescript` + `openapi-fetch` (or Orval) consuming the backend `openapi.json` to ensure **100% End-to-End Type Safety** without manually typing interfaces.
- **Forms and Validation**: **React Hook Form** + **Zod** (schemas mirroring API validation rules).
- **UI & Styling**: **Tailwind CSS** + **shadcn/ui** (accessible WAI-ARIA, open-source, customizable).
- **Authentication**: `@supabase/ssr` / `@supabase/supabase-js` automatically injecting the `Bearer <access_token>` into requests to the C# API.

---

## 3. Agentic Pipeline Workflow (Discovery -> Spec -> DoR -> Code -> QA/Sec -> DoD -> PR)

All development must respect the **Quality Gates** below. No agent may skip phases:

### Gate 1: Discovery, Requirements, and Risk Forecasting (`product-requirements-analyst`)
- Analyze workspace structure or user requests.
- Gather **Functional Requirements (FR)** and **Non-Functional Requirements (NFR)** (SLAs, latency, throughput, cost, data retention, Web/Mobile compatibility).
- Map **Risks, Edge Cases, and Foreseen Failure Modes** with their respective **Recommended Solutions**.
- List all blocking **Open Questions**. If critical ambiguity exists, interview the user before proceeding or document the default architectural decision (ADR).

### Gate 2: Architecture, Technical Spec, and Tasks (`domain-architect-planner`)
- Model **Bounded Contexts**, **Aggregates**, **Value Objects**, **Domain Events**, and **Integration Events (RabbitMQ)**.
- Define REST contracts (routes, payloads, status codes, idempotency keys) and PostgreSQL database schemas (indexes, constraints).
- Decompose work into **Atomic Tasks** ordered by dependency.
- Validate the **Definition of Ready (DoR)** (see `docs/pipeline/DEFINITION_OF_READY.md`). No task proceeds to coding without meeting 100% of DoR.

### Gate 3: Test-Driven Implementation (`backend-csharp-developer` & `frontend-web-developer`)
- Implement strictly following the Spec and Hexagonal Architecture.
- **Mandatory Automated Tests**:
  - **Backend (.NET)**: `xUnit` + `FluentAssertions` + `NSubstitute` (Unit tests for Domain/Application) + `Testcontainers` (`PostgreSql`, `RabbitMq`) and `WebApplicationFactory` (Real API and messaging integration tests).
  - **Frontend (TS)**: `Vitest` + `React Testing Library` + `MSW (Mock Service Worker)` (Unit/Integration tests for components and API hooks) + `Playwright` (Critical E2E flows).

### Gate 4: Quality Audit, OWASP Security, and PR (`qa-security-reviewer` & `devops-ci-engineer`)
- Validate against **OWASP Top 10** and **OWASP API Security Top 10** (BOLA/IDOR, Broken Auth, Mass Assignment, Injection, Rate Limiting, Secrets Exposure).
- Run Linters and Formatters (`dotnet format`, Roslyn Analyzers, `eslint`, `tsc --noEmit`).
- Validate the **Definition of Done (DoD)** (see `docs/pipeline/DEFINITION_OF_DONE.md`).
- Generate a comprehensive Pull Request report containing: Executive Summary, Architecture/Decisions, Test Execution Evidence, OWASP Checklist, and Rollback Guide.

---

## 4. Map of Specialized Agents, Skills, and Documentation

Consult and engage the resources below according to the pipeline phase:

- **Specialized Agents (`.agents/agents/`)**:
  - `pipeline-orchestrator`: Pipeline conductor (coordinates Discovery to PR).
  - `product-requirements-analyst`: Specialist in Discovery, FR/NFR, Open Questions, Failure Modes, and DoR.
  - `domain-architect-planner`: DDD, Hexagonal Architecture, EDA (RabbitMQ), PostgreSQL, REST API, and Task decomposition architect.
  - `backend-csharp-developer`: C# .NET Minimal API, DDD, EF Core, MassTransit/RabbitMQ, Cache, and xUnit/Testcontainers senior engineer.
  - `frontend-web-developer`: React/Next.js, TypeScript, TanStack Query, Zod, Supabase Auth, and Vitest/Playwright senior engineer.
  - `qa-security-reviewer`: Quality auditor enforcing Clean Code, KISS, Testing, OWASP API Top 10, and PR reviews.
  - `devops-ci-engineer`: Platform engineer for GitHub Actions (CI/CD), Docker, quality automation, and FinOps cost controls.
- **Operational Skills (`.agents/skills/`)**:
  - `discovery-and-spec`: Transforming an idea or workspace into a full technical Spec, FR/NFR, ADRs, and atomic Tasks without ambiguity.
  - `csharp-hexagonal-ddd`: Code recipes and standards for C# Minimal API, Hexagonal Architecture, Result Pattern, RabbitMQ Outbox, Cache, and Supabase Auth.
  - `frontend-api-architecture`: Modern frontend architecture for consuming REST APIs, TanStack Query, OpenAPI codegen, and MSW/Vitest testing.
  - `owasp-quality-gate`: Executable security checklist (OWASP), linting, test coverage, and automated code review.
  - `pr-delivery-automation`: Preparing semantic commits, executing local/CI validation test suites, and publishing detailed GitHub PRs.
  - `sre-observability-telemetry`: SRE standards, OpenTelemetry (Tracing, Metrics, Structured Logging), Grafana / Aspire Dashboard, and RCA runbooks.
  - `run-agentic-pipeline`: Multi-agent autonomous end-to-end orchestration.
- **Documentation and Templates (`docs/`)**:
  - `docs/pipeline/DEFINITION_OF_READY.md` & `docs/pipeline/DEFINITION_OF_DONE.md`
  - `docs/templates/SPEC_TEMPLATE.md`, `docs/templates/ADR_TEMPLATE.md`, `docs/templates/TASKS_TEMPLATE.md`
  - `docs/architecture/STACK_AND_DECISIONS.md`

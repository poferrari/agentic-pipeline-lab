## 🎯 1. Executive Summary and Context
- **Spec / Related Requirement**: `docs/specs/[spec-name].md`
- **Completed Tasks**: `docs/specs/[spec-name]-tasks.md`
- **Delivery Objective**:
  <!-- Clearly and concisely describe what was built and what value it delivers to the MVP -->

---

## 🏗️ 2. Architecture and Layered Changes (Hexagonal + DDD + EDA)

| Layer | Files / Components Created or Modified | Applied Technical Decision |
| :--- | :--- | :--- |
| **`Domain` (C# Core)** | `...` | Aggregates with protected invariants, immutable Value Objects, zero external dependencies. |
| **`Application` (Use Cases)** | `...` | Commands/Queries, `FluentValidation`, strict `Result<T>` returns. |
| **`Infrastructure` (Adapters)** | `...` | EF Core (PostgreSQL), MassTransit Outbox (RabbitMQ), `HybridCache`, Supabase Auth JWT. |
| **`Minimal API` (Driving)** | `...` | Endpoints with `TypedResults`, RFC 7807 `ProblemDetails`, and OpenAPI v1 contract. |
| **`Frontend` (Web)** | `...` | `TanStack Query v5` hooks, `Zod` schemas, Supabase auth integration, and full state handling (`loading/empty/error/success`). |

---

## 🛡️ 3. Problem Forecasting & Implemented Mitigations
- **Concurrency / Idempotency**: <!-- e.g. Idempotency-Key headers and Transactional Outbox Pattern -->
- **Data Isolation (BOLA / IDOR)**: <!-- e.g. All queries filter by UserId extracted from validated Supabase JWT -->
- **Performance & Caching**: <!-- e.g. AsNoTracking paginated queries cached via HybridCache -->

---

## 🧪 4. Verified Executed Test Results

| Test Suite | Tooling | Total | Passed | Failed | Coverage / Status |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **Backend Unit Tests** (`Domain` & `Application`) | `xUnit` + `FluentAssertions` + `NSubstitute` | `0` | `0` | `0` | ✅ Passed |
| **Backend Architecture Tests** (Hexagonal Rules) | `NetArchTest` | `0` | `0` | `0` | ✅ Passed |
| **Backend Integration Tests** (`API + PG + RabbitMQ`) | `WebApplicationFactory` + `Testcontainers` | `0` | `0` | `0` | ✅ Passed |
| **Frontend Unit & API Tests** (`Components + Hooks`) | `Vitest` + `Testing Library` + `MSW` | `0` | `0` | `0` | ✅ Passed |

<details>
<summary>📋 <b>Click to expand validation test execution log (`validate-all.ps1`)</b></summary>

```text
Paste summary test command output here
```
</details>

---

## 🔒 5. Security Checklist (OWASP API Top 10) and Definition of Done (DoD)

- [ ] **DoR & Spec**: Code is 100% compliant with Spec, with zero ambiguity or scope creep (KISS / Clean Code).
- [ ] **API1 (BOLA / IDOR)**: Endpoints enforce resource ownership (`OwnerId == CurrentUser.Id`).
- [ ] **API2 & API5 (AuthN / AuthZ)**: Supabase JWT validation active and `.RequireAuthorization()` enforced.
- [ ] **API3 (Mass Assignment)**: Exclusive use of immutable `record` DTOs in Minimal API parameter bindings.
- [ ] **API4 (Resource Consumption)**: Page size caps enforced (`MaxPageSize <= 100`) and Rate Limiting active.
- [ ] **API8 (Secrets & Config)**: Zero hardcoded secrets, keys, or passwords; standardized `ProblemDetails`.
- [ ] **Zero Warnings**: `dotnet build -warnaserror`, `dotnet format`, `eslint`, and `tsc --noEmit` pass with zero warnings.

---

## 🚀 6. Deployment, Database Migrations, and Rollback Guide
- **Database Migrations (PostgreSQL)**: `[Yes / No - describe command or schema impact]`
- **New Messaging Queues / Events (RabbitMQ)**: `[Yes / No]`
- **Rollback Procedure**: `[Safe operational steps if rollback is needed]`

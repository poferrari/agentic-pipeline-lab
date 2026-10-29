# Technical and Functional Specification (Spec): `[FEATURE_OR_PROJECT_NAME]`

> **Status**: `[DRAFT | IN_REVIEW | READY_FOR_DEV | IMPLEMENTED]`  
> **Date**: `YYYY-MM-DD`  
> **Authors**: `product-requirements-analyst` & `domain-architect-planner`  

---

## 1. Context, Problem Statement, and Business Objective (MVP)
- **Problem**: What real customer pain are we solving?
- **MVP Objective**: What is the minimal, high-value, low-cost delivery that validates this capability?
- **Out of Scope (Non-Goals / YAGNI)**: What did we explicitly decide **NOT** to build right now to maintain simplicity (KISS)?

---

## 2. Open Questions, Assumptions, and Decisions
> **Rule**: No task may begin development if a blocking `[OPEN QUESTION]` remains unanswered.

| ID | Topic / Question | Status (`OPEN` / `RESOLVED`) | Agreed Decision / Solution |
| :--- | :--- | :--- | :--- |
| `OQ-01` | e.g. What is the retry limit before throttling? | `RESOLVED` | Capped at 5 attempts per 15 minutes via Rate Limiter. |

---

## 3. Functional Requirements (FR) and Acceptance Criteria (BDD)

### `FR-001`: [Concise Requirement Title]
- **Description**: The system must...
- **Actors / Roles (Supabase Auth)**: `[Anonymous | Authenticated User | Admin]`
- **Acceptance Criteria (Gherkin)**:
  - **Scenario 1 (Happy Path)**:
    - **Given** that...
    - **When**...
    - **Then**...
  - **Scenario 2 (Business Rule / Expected Failure)**:
    - **Given** that...
    - **When**...
    - **Then** it must return HTTP `400/422` with error code `Domain.RuleViolated`.

---

## 4. Non-Functional Requirements (NFR) and Constraints
- **Performance & Latency**: `p95 < 200ms` for read queries; asynchronous background operations offloaded to RabbitMQ.
- **Scalability & Cross-Platform**: Stateless API serving both current Web and future Mobile clients.
- **Security (OWASP)**: Defense against BOLA/IDOR (mandatory filter by `UserId` extracted from JWT), Rate Limiting, strict payload validation.
- **Cost (FinOps)**: Exclusively leverage already-provisioned database (PostgreSQL), messaging (RabbitMQ), and auth (Supabase).

---

## 5. Preventative Risk Analysis, Edge Cases, and Mitigations

| ID | Foreseen Failure Mode / Edge Case | Impact | Technical Solution Built into Spec |
| :--- | :--- | :--- | :--- |
| `RISK-01` | User double-click or network retry causing duplicate records | High | Use `Idempotency-Key` or PostgreSQL unique constraint. |
| `RISK-02` | Database commits transaction, but RabbitMQ is temporarily down during publish | High | Mandatory **Transactional Outbox Pattern** with MassTransit + EF Core. |
| `RISK-03` | User attempts to access record belonging to another tenant/user (IDOR) | Critical | Repository query strictly filters by `(Id == request.Id && OwnerId == currentUser.Id)`. |

---

## 6. Domain Modeling (DDD) and Hexagonal Architecture

### 6.1. Bounded Context & Ubiquitous Language
- **Context**: `[ContextName]`
- **Aggregate Root**: `[AggregateName]`
  - **Properties & Value Objects**: e.g. `Email`, `Money`, `OrderId` (`readonly record struct`).
  - **Invariants (Protected Rules)**:
    1. `[Rule 1]`
    2. `[Rule 2]`
  - **Emitted Domain Events**: `[e.g. OrderCreatedDomainEvent]`

### 6.2. Ports (Interfaces) and Use Cases (Application Layer)
- **Driving Ports (Commands / Queries)**:
  - `CreateXCommand(string Prop1, decimal Prop2) : ICommand<Result<CreateXResponse>>`
  - `GetXByIdQuery(Guid Id) : IQuery<Result<XDto>>`
- **Validators (`FluentValidation`)**:
  - `Prop1`: `NotEmpty()`, `MaximumLength(100)`
- **Driven Ports (Domain/Application Interfaces)**:
  - `IXRepository`
  - `IUnitOfWork`

---

## 7. REST API Contracts (Web & Mobile)

### `POST /api/v1/[resource]`
- **Authentication**: `Bearer <Supabase_JWT>` (`RequireAuthorization`)
- **Request Body (`application/json`)**:
  ```json
  {
    "fieldA": "string",
    "fieldB": 123.45
  }
  ```
- **Responses**:
  - `201 Created`: Returns `Location` header and created resource payload.
  - `400 Bad Request` / `422 Unprocessable Entity`: Returns `ProblemDetails` (RFC 7807) with validation error dictionary.
  - `401 Unauthorized` / `403 Forbidden`.
  - `409 Conflict`: Duplicate resource.

---

## 8. Asynchronous Messaging (RabbitMQ) and Caching

### 8.1. Integration Events (RabbitMQ / MassTransit)
- **Event**: `[EventNameIntegrationEvent]`
- **Exchange / Queue**: `[queue-name]`
- **Producer**: `[Command publishing via Outbox]`
- **Consumer**: `[Processing consumer]`
- **Idempotency Guarantee**: `[How consumer avoids processing the same message twice]`
- **Retry & DLQ Policy**: `3 exponential retries (2s, 5s, 15s) -> _error queue`.

### 8.2. Caching Strategy (`HybridCache`)
- **Cache Key**: `"[context]:[entity]:{id}"`
- **TTL**: `L1 = 5 min`, `L2 = 15 min`
- **Invalidation**: Evict cache key on `Update` or `Delete`.

---

## 9. Data Modeling (PostgreSQL)

```sql
CREATE TABLE table_name (
    id UUID PRIMARY KEY,
    owner_id UUID NOT NULL,
    status VARCHAR(30) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL
);

CREATE INDEX idx_table_name_owner_id ON table_name (owner_id);
```

---

## 10. Frontend Architecture (Next.js / React + TanStack Query)
- **Routes / Pages**: `/[route]`
- **UI Components**: `[List of clean, reusable components]`
- **TanStack Query Hooks**:
  - `useGet[Resource](id)` (`queryKey: ['resource', id]`)
  - `useCreate[Resource]()` (invalidates `['resource']` in `onSuccess`)
- **Mandatory UI States**:
  - Skeleton/Loading state
  - Empty state (when query returns `[]`)
  - User-friendly error state (parsing `ProblemDetails`)
  - Visual success feedback (Toast)

---

## 11. Automated Test Plan

| Layer | Test Type | Tool | Mandatory Scenarios to Verify |
| :--- | :--- | :--- | :--- |
| **Backend Domain** | Unit | `xUnit` + `FluentAssertions` | Aggregate instantiation, invariant violation enforcement, Value Object equality. |
| **Backend App** | Unit | `xUnit` + `NSubstitute` | Handler success, FluentValidation errors, `Result.Failure` mapping. |
| **Backend Arch** | Architecture | `NetArchTest` | `Domain` has zero references to `Infrastructure` or `Api`. |
| **Backend API/Infra** | Integration | `Testcontainers` + `WebApplicationFactory` | POST `201` persisting to Postgres, Outbox publishing to RabbitMQ, `401`/`403` BOLA blocking. |
| **Frontend** | Unit / Integration | `Vitest` + `Testing Library` + `MSW` | Data rendering, Zod form validation, API error handling (400/500). |
| **End-to-End** | E2E | `Playwright` | Complete critical user journey. |

---

## 12. Definition of Ready (DoR) Sign-Off
- [ ] All checklist items in `docs/pipeline/DEFINITION_OF_READY.md` verified and approved.

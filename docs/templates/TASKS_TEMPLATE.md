# Task Execution Plan and Atomic Decomposition: `[FEATURE_NAME]`

> **Reference Spec**: `docs/specs/[spec-name].md`  
> **Execution Rule**: Tasks are atomic and sequential. An agent only advances to the next task when the **Verification Command** of the current task succeeds with **0 errors**.

---

## Progress Summary

| Task ID | Layer | Task Title | Assigned Agent | Status (`TODO` \| `IN_PROGRESS` \| `DONE`) |
| :--- | :--- | :--- | :--- | :--- |
| `TASK-01` | Domain | Model Aggregate, Value Objects, Domain Events, and Unit Tests | `backend-csharp-developer` | `TODO` |
| `TASK-02` | Application | Implement Commands, Queries, Validators (FluentValidation), and Tests | `backend-csharp-developer` | `TODO` |
| `TASK-03` | Infrastructure | Map EF Core (PostgreSQL), Migrations, RabbitMQ Outbox, Cache, and Auth | `backend-csharp-developer` | `TODO` |
| `TASK-04` | API & Integration | Expose Minimal API Endpoints, OpenAPI, and Integration Tests (Testcontainers) | `backend-csharp-developer` | `TODO` |
| `TASK-05` | Frontend | Derive OpenAPI Types, TanStack Query Hooks, UI, Zod Forms, and MSW Tests | `frontend-web-developer` | `TODO` |
| `TASK-06` | QA & Security | Execute OWASP Audit, Architecture Tests, Linters, and PR Report | `qa-security-reviewer` | `TODO` |

---

## Atomic Task Details

### `TASK-01`: Domain Core Modeling and Unit Tests
- **Objective**: Implement entities, Value Objects, domain errors, and repository interfaces with zero external framework dependencies.
- **Files to Create/Modify**:
  - `src/backend/src/App.Domain/[Context]/[Aggregate].cs`
  - `src/backend/src/App.Domain/[Context]/ValueObjects/[ValueObject].cs`
  - `src/backend/src/App.Domain/[Context]/I[Aggregate]Repository.cs`
  - `src/backend/tests/App.UnitTests/Domain/[Aggregate]Tests.cs`
- **Task Done Criteria**:
  - Private constructors with factory methods returning `Result<T>`.
  - 100% of domain invariants covered by unit tests in `xUnit`.
- **Verification Command**:
  ```powershell
  dotnet test src/backend/tests/App.UnitTests --filter "FullyQualifiedName~Domain"
  ```

### `TASK-02`: Application Use Cases, Validators, and Handler Tests
- **Objective**: Implement Command/Query Handlers, `FluentValidation`, and DTO contracts.
- **Files to Create/Modify**:
  - `src/backend/src/App.Application/[Context]/[Command]/[Command]Command.cs`
  - `src/backend/src/App.Application/[Context]/[Command]/[Command]Validator.cs`
  - `src/backend/src/App.Application/[Context]/[Command]/[Command]Handler.cs`
  - `src/backend/tests/App.UnitTests/Application/[Command]HandlerTests.cs`
- **Verification Command**:
  ```powershell
  dotnet test src/backend/tests/App.UnitTests --filter "FullyQualifiedName~Application"
  ```

### `TASK-03`: Infrastructure (EF Core PostgreSQL, RabbitMQ MassTransit, HybridCache)
- **Objective**: Implement Driven Adapters (`IEntityTypeConfiguration`, Repositories, RabbitMQ Outbox consumers).
- **Files to Create/Modify**:
  - `src/backend/src/App.Infrastructure/Persistence/Configurations/[Aggregate]Configuration.cs`
  - `src/backend/src/App.Infrastructure/Persistence/Repositories/[Aggregate]Repository.cs`
  - `src/backend/src/App.Infrastructure/Messaging/[Event]Consumer.cs`
- **Verification Command**:
  ```powershell
  dotnet build src/backend -warnaserror
  ```

### `TASK-04`: Minimal API Endpoints, JWT Security, and Integration Tests
- **Objective**: Expose HTTP routes in Minimal API with `TypedResults`, `.RequireAuthorization()`, `ProblemDetails`, and validate via `Testcontainers`.
- **Files to Create/Modify**:
  - `src/backend/src/App.Api/Endpoints/[Context]Endpoints.cs`
  - `src/backend/tests/App.IntegrationTests/[Context]/[Context]ApiTests.cs`
  - `src/backend/tests/App.ArchTests/HexagonalArchitectureTests.cs`
- **Verification Command**:
  ```powershell
  dotnet test src/backend
  ```

### `TASK-05`: Frontend Integration (OpenAPI Types, TanStack Query, Components, Vitest)
- **Objective**: Synchronize OpenAPI contracts, create query/mutation hooks, UI views, and tests with `Vitest` + `MSW`.
- **Files to Create/Modify**:
  - `src/frontend/src/features/[context]/api/use[Resource].ts`
  - `src/frontend/src/features/[context]/components/[Component].tsx`
  - `src/frontend/src/features/[context]/__tests__/[Component].test.tsx`
- **Verification Command**:
  ```powershell
  npm --prefix src/frontend run lint; npm --prefix src/frontend run typecheck; npm --prefix src/frontend run test:run
  ```

### `TASK-06`: Final Quality Gate, OWASP Review, and Pull Request Preparation
- **Objective**: Verify Definition of Done (DoD), check OWASP Top 10 vulnerabilities, and generate the final PR report.
- **Verification Command**:
  ```powershell
  pwsh -File scripts/validate-all.ps1
  ```

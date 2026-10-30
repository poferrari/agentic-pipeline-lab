# Definition of Done (DoD) — Delivery Criteria and Pull Request Standards

No Task, Feature, or Pull Request is considered **Done** without **100% of applicable items below** verified through automated checks and audited by `qa-security-reviewer`.

---

## 1. Code Quality and Architecture (KISS, Clean Code, Hexagonal)
- [ ] **Hexagonal Isolation & DDD**:
  - The `Domain` layer has zero dependencies on infrastructure packages (`Microsoft.EntityFrameworkCore`, `MassTransit`, `AspNetCore`, etc.).
  - Entities and Aggregates protect their invariants (private constructors/factory methods, collections exposed as `IReadOnlyCollection<T>`).
  - Business errors utilize the **Result Pattern** (`Result<T>`) and return standardized `ProblemDetails` (RFC 7807) in the Minimal API; no exceptions are thrown for expected business validation flow.
- [ ] **KISS & Clean Code**:
  - Zero dead code, commented-out blocks, unassigned `TODO` comments, or stray `Console.WriteLine` / `console.log` statements.
  - Cohesive methods, expressive naming, zero accidental duplication.
- [ ] **Zero Compilation and Linter Warnings**:
  - Backend: `dotnet build --configuration Release -warnaserror` and `dotnet format --verify-no-changes` pass with **0 errors and 0 warnings**.
  - Frontend: `npm run lint`, `npm run typecheck` (`tsc --noEmit`), and `npm run build` pass with **0 errors and 0 warnings**.

## 2. Automated Testing (Unit, Integration, and Contract)
- [ ] **Unit Tests (Backend & Frontend)**:
  - All unit tests pass (`dotnet test --filter Category=Unit` and `npm run test:unit`).
  - Domain rules, Value Objects, Command/Query Handlers, and Validators maintain >= 85% coverage on new modules.
- [ ] **Integration Tests**:
  - Backend: Minimal API endpoints, PostgreSQL persistence, and RabbitMQ message publish/consume flows are verified via `WebApplicationFactory` + `Testcontainers` (or isolated test environment).
  - Frontend: Components, views, and `TanStack Query` hooks are tested using `Vitest` + `React Testing Library` + `MSW` covering `isLoading`, `isSuccess`, `isEmpty`, and `isError` states.
- [ ] **Contract Synchronization (Web & Mobile Ready)**:
  - The OpenAPI/Swagger specification is synchronized with TypeScript frontend types.

## 3. Resilience, Messaging, and Performance
- [ ] **Idempotency and Consistency (RabbitMQ & DB)**:
  - Event publishing employs the **Transactional Outbox Pattern** (or equivalent atomic guarantee).
  - RabbitMQ consumers are idempotent and implement exponential retry backoff.
- [ ] **Optimized Queries and Caching**:
  - EF Core read queries apply `.AsNoTracking()`, projections (`.Select()`), and page size caps. Zero N+1 queries introduced.
  - Caching (`HybridCache` / `TanStack Query`) configured with proper TTL and invalidation triggers on state mutations.

## 4. OWASP Security (API & Web Top 10)
- [ ] **API1:2023 - Broken Object Level Authorization (BOLA/IDOR)**: All routes accessing records by ID verify that the authenticated user (Supabase JWT) strictly owns the record.
- [ ] **API2:2023 & API5:2023 - Broken Authentication & Function Level Authorization**: Protected endpoints enforce `.RequireAuthorization()`; public routes have intentional and documented `.AllowAnonymous()`.
- [ ] **API3:2023 - Broken Object Property Level Authorization (Mass Assignment)**: Endpoints never bind database entities (`DbContext`) directly from HTTP request bodies; dedicated immutable Request DTOs are used.
- [ ] **API4:2023 - Unrestricted Resource Consumption**: Max page size enforced (e.g. `MaxPageSize = 100`) and Rate Limiting active.
- [ ] **API8:2023 - Security Misconfiguration & Secrets**:
  - Zero hardcoded credentials, Supabase service keys, connection strings, or secrets in code or git history.
  - Structured logs do not leak PII, passwords, or raw JWT tokens.

## 5. CI/CD and Pull Request Documentation
- [ ] **Green GitHub Actions Pipeline**: All CI jobs for build, linting, tests, and static security analysis (CodeQL / Dependency Review / Trivy) pass.
- [ ] **Safe Migrations**: If database changes occurred, the EF Core migration was generated, reviewed, and tested (up and down).
- [ ] **Mandatory Technical Language Standard (EN-US)**: All source code, class/method names, comments, structured logs, test names, semantic commits, and PR descriptions are 100% in technical English (EN-US).
- [ ] **Comprehensive Pull Request Report**: The PR description follows the template from `pr-delivery-automation` (Executive Summary, Architectural Decisions, Executed Test Evidence, OWASP Checklist, and Deployment/Rollback Instructions).

## 6. User Experience (UX), Usability & Accessibility Gate
- [ ] **The 4 Data States Implemented**: Every async component provides seamless structural skeleton loaders (`animate-pulse`), helpful empty states with a primary CTA, empathetic error messages with a 1-click retry action, and clean data views.
- [ ] **Perceived Performance & Layout Stability**: Interactive actions provide immediate visual feedback (optimistic UI or loading indicators) with zero layout shifting (Cumulative Layout Shift `CLS = 0.000`).
- [ ] **UX Writing & ProblemDetails Translation**: No raw HTTP status codes or stack traces are displayed to end users; API errors (RFC 7807) are translated into actionable, empathetic guidance.
- [ ] **Keyboard Navigability & Focus Management**: Overlays, dialogs, and drawers trap focus, close cleanly via `Escape`, return focus to the trigger element, and support full `Tab` / `Shift+Tab` / `Enter` navigation.
- [ ] **WCAG 2.1 AA & WCAG 2.5.3 Compliance**: Color contrast ratio exceeds 4.5:1 (5.5:1+ for dark mode), button `aria-label` starts with the visible button text (*Label in Name*), and dynamic tickers use `aria-live="polite"`.

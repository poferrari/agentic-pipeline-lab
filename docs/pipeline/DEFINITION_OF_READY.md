# Definition of Ready (DoR) — Readiness Criteria for Development

No Task or Feature may proceed to the **Implementation Phase (`backend-csharp-developer` / `frontend-web-developer`)** without **100% of the criteria below** checked and validated in the Technical Specification.

The purpose of the **DoR** is to eliminate rework, ambiguity, and guesswork during coding.

---

## 1. Business and Scope Clarity (Zero Ambiguity)
- [ ] **Objective & Business Value**: Clearly articulated *why* this feature exists and what user problem (MVP) it solves.
- [ ] **Functional Requirements (FR)**: All primary flows (Happy Path), alternative flows, and business edge cases are enumerated with unique identifiers (`FR-001`, `FR-002`).
- [ ] **Resolved Open Questions**: No open functional questions remain (`[OPEN QUESTION]` = 0). If an engineering default was assumed, it is documented as a Validated Assumption or ADR.
- [ ] **Acceptance Criteria (BDD / Gherkin)**: Every requirement has explicit scenarios in `Given / When / Then` format, including error cases.

## 2. Architecture and Domain Modeling (DDD + Hexagonal + Event-Driven)
- [ ] **Ubiquitous Language**: Domain concepts (Entities, Aggregates, Value Objects, Events) are named consistently without conflicting synonyms.
- [ ] **Domain Invariants**: Business invariants protected by the Aggregate Root are explicit (e.g. "A paid order cannot have items removed").
- [ ] **Inbound & Outbound Ports (Hexagonal)**:
  - Which Commands/Queries in the `Application` layer will be implemented?
  - Which outbound ports (`IRepository`, `ICache`, `IMessageBus`) will be invoked?
- [ ] **Messaging & Events (RabbitMQ)**:
  - If asynchronous communication is needed, the event name (`<Context>.<Entity><Action>Event`), JSON payload schema, exchange/queue naming, **Idempotency Strategy**, **Transactional Outbox**, and **Dead-Letter Queue (DLQ)** are fully specified.

## 3. REST API Contracts (Web & Mobile) and Database Schema
- [ ] **HTTP Contract (OpenAPI)**:
  - Method (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`), versioned route (`/api/v1/...`), Request Body, Query Params, and Response DTOs specified.
  - Standard HTTP status codes mapped (`200`, `201`, `204`, `400 ProblemDetails`, `401`, `403`, `404`, `409`, `422`, `429`).
- [ ] **Input Validation**: Explicit validation rules for `FluentValidation` (backend) and `Zod` (frontend) documented (max lengths, formats, required fields).
- [ ] **Data Modeling (PostgreSQL)**:
  - Tables, columns, data types, primary keys (`UUID` / `Guid`), foreign keys, and **performance composite indexes** specified.
  - Migration backwards-compatibility evaluated.

## 4. Non-Functional Requirements (NFR), Cost, and Security (OWASP)
- [ ] **Authentication & Authorization (Supabase Auth)**:
  - Which JWT roles/claims are authorized to access this resource?
  - How is **BOLA / IDOR** prevented (e.g. validating that token `sub` / `user_id` strictly owns the record)?
- [ ] **Caching Strategy**:
  - Are results cached? What are the cache key, TTL, and cache invalidation triggers?
- [ ] **Failure Forecasting and Mitigation**:
  - What happens if the database is unavailable, RabbitMQ is delayed, or a user double-clicks (optimistic concurrency / `Idempotency-Key`)?
- [ ] **MVP Cost Alignment**: The solution utilizes free-tier or open-source stack capabilities without introducing unnecessary paid services.

## 5. Test Plan and Atomic Task Decomposition
- [ ] **Mapped Test Plan**:
  - List of unit tests (`Domain` invariants and `Application` handlers/validators) to be written.
  - List of integration tests (`Api` + `PostgreSQL` + `RabbitMQ` / `MSW` for frontend) to be written.
- [ ] **Atomic and Independent Tasks**: Work is broken into small, sequential tasks with clear file boundaries and verification commands.
- [ ] **Mandatory Technical Language Standard (EN-US)**: The technical specification (`docs/specs/`), user stories, acceptance criteria, ubiquitous language terms, and task descriptions are strictly written in technical English (EN-US).

## 6. User Experience (UX), Usability & Accessibility Planning
- [ ] **The 4 Data States Mapped**: The technical specification documents explicit designs for: Loading (structural skeleton), Empty State (friendly message + actionable CTA), Error State (human-readable RFC 7807 ProblemDetails + 1-click retry button), and Success/Data State.
- [ ] **Cognitive Usability & Touch Targets**: Primary user journeys respect Fitts's Law (minimum 44x44px touch targets on mobile) and Hick's Law (progressive disclosure for advanced settings to minimize choice overload).
- [ ] **Destructive Action Safety**: High-impact or destructive actions (deletion, state reset) include either an explicit confirmation step or an immediate undo mechanism (e.g. 5-second toast).
- [ ] **Accessibility (a11y) Architecture**: Keyboard navigation flow (`Tab`, `Shift+Tab`, `Escape` to close overlays), accessible button naming, and WCAG 2.1 AA color contrast compliance planned upfront.

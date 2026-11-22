---
name: discovery-and-spec
description: >-
  Use this skill at the beginning of any new project, epic, or feature to analyze repository structure,
  consult the Memory Bank, gather Functional and Non-Functional Requirements (FR/NFR), forecast failure modes,
  enforce the 4 Pillars of Spec-Driven Development (SDD: Clarity, Evidence, Independence, Continuity),
  design the technical Spec, and validate the Definition of Ready (DoR) and atomic Task plan prior to coding.
---

# Proactive Discovery, Spec-Driven Development, and Task Decomposition (`discovery-and-spec`)

This skill transforms any briefing, issue, or request into a **Complete and Unambiguous Engineering Plan** governed by the **4 Pillars of Spec-Driven Development (SDD)**:
1. **Clarity**: Explicit requirements leaving zero room for model assumptions or hallucinations.
2. **Evidence**: Every acceptance criterion mapped to deterministic automated tests.
3. **Independence**: Strict role separation between specification, implementation, and quality auditing.
4. **Continuity**: Continuous synchronization with the repository's living **Memory Bank** (`.agents/memory/`).

---

## Step 1: Memory Ingestion and Context Inspection

1. **Load Memory Bank**:
   - Read `.agents/memory/systemPatterns.md` to align with established architectures (Hexagonal, Minimal APIs, Result Pattern, Zod, Supabase Auth).
   - Read `.agents/memory/learnedLessons.md` to avoid previously documented traps and anti-patterns.
   - Check `.agents/memory/activeContext.md` to see current work streams.
2. **Evaluate Computational vs. Inferential Guides**:
   - Verify local computational tools available on the host (e.g. `dotnet`, `node`, `npm`, `docker`). Use CPU-driven local checks (`dotnet build`, `tsc --noEmit`) to validate environments with zero token waste.
3. **Assess Task Impact & Select Workflow Tier**:
   - **Low Impact** (Local tweak, documentation, styling): Modular flow $\rightarrow$ Spec + Direct Execution.
   - **Medium/High Impact** (New endpoint, schema migration, messaging, security flow): Full 4-Stage Flow $\rightarrow$ `Specification` $\rightarrow$ `Design (ADRs)` $\rightarrow$ `Atomic Tasks` $\rightarrow$ `Execution`.

---

## Step 2: Preventative Analysis (Pre-Mortem Failure Mode Matrix)

Always verify and answer how the system will handle the 8 critical points below:
1. **Concurrency and Duplication**: Prevent duplicate processing via `Idempotency-Key` or database unique constraints with `409 Conflict`.
2. **Dual-Write Failure (DB + RabbitMQ)**: Guarantee consistency via the **Transactional Outbox Pattern** (MassTransit EF Core Outbox).
3. **Duplicate or Poison Messages in RabbitMQ**: Ensure consumer idempotency and configure retry policies with Dead-Letter Queues (`_error`).
4. **Multi-Tenant Security (BOLA/IDOR)**: Enforce row-level ownership filtering using claims extracted from validated Supabase JWTs.
5. **Read Explosion / Database Latency**: Enforce mandatory pagination (`pageSize <= 100`), composite PostgreSQL indexes, and `HybridCache`.
6. **Platform Evolution (Web & Mobile)**: Provide platform-agnostic REST contracts documented with typed OpenAPI 3.1; avoid browser-cookie lock-in.
7. **Infrastructure FinOps**: Prioritize free-tier commodities (Supabase Auth, unified Postgres, CloudAMQP/local Docker, In-Memory L1 Cache).
8. **Anti-Crash Guard Discipline**: Model domain types so illegal states cannot compile; prohibit superficial null-checks that hide illegal states.

---

## Step 3: Open Questions Resolution

1. Identify all ambiguities.
2. For architectural/technical decisions, adopt the documented baseline in `docs/architecture/STACK_AND_DECISIONS.md`.
3. For business ambiguities, ask the user using the interactive question tool (`AskUserQuestion` in Claude Code, `ask_question` in Gemini) with a lean MVP recommendation (`(Recommended)`).

---

## Step 4: Spec Generation and Atomic Task Plan

1. Create `docs/specs/<feature-slug>.md` completing all sections of [SPEC_TEMPLATE.md](../../docs/templates/SPEC_TEMPLATE.md).
   - Specify exact classes, records, HTTP routes, status codes, RabbitMQ events, and SQL migrations.
2. Create `docs/specs/<feature-slug>-tasks.md` completing [TASKS_TEMPLATE.md](../../docs/templates/TASKS_TEMPLATE.md).
   - Ensure tasks are decomposed into small atomic units (avoid massive 10,000-line changes).
3. Strictly verify every item in [DEFINITION_OF_READY.md](../../docs/pipeline/DEFINITION_OF_READY.md). Only proceed to coding when 100% of DoR is satisfied.

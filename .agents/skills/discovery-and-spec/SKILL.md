---
name: discovery-and-spec
description: >-
  Use this skill at the beginning of any new project, epic, or feature to analyze repository structure,
  gather Functional and Non-Functional Requirements (FR/NFR), forecast problems and solutions, resolve Open Questions,
  design the full technical Spec (DDD, Hexagonal, RabbitMQ, PostgreSQL, REST API, Frontend), and validate the
  Definition of Ready (DoR) and atomic Task plan prior to coding.
---

# Proactive Discovery, Technical Specification, and Task Decomposition (`discovery-and-spec`)

This skill transforms any project structure, briefing, or user request into a **Complete and Unambiguous Engineering Plan**, ensuring subsequent implementing agents code without doubts, rework, or architectural flaws.

---

## Step 1: Context Inspection and Preventative Analysis (Pre-Mortem)

1. **Inspect Workspace**:
   - Read existing files in the project folder (documents, diagrams, schemas, legacy code, or drafts).
2. **Apply the Proactive Problem Predictor Matrix**:
   Always verify and answer in the Spec how the system will handle the 8 critical points below:
   - **Concurrency and Duplication**: What if the user double-clicks or the network resends the POST request? *(Standard solution: `Idempotency-Key` header or `UNIQUE` constraint in PostgreSQL + `409 Conflict` capture)*.
   - **Dual-Write Failure (DB + RabbitMQ)**: What if the PostgreSQL commit succeeds but RabbitMQ is temporarily unreachable? *(Standard solution: **Transactional Outbox Pattern** via MassTransit EF Core Outbox)*.
   - **Duplicate or Poison Messages in RabbitMQ**: What if the consumer receives the same event twice or fails 3 times consecutively? *(Standard solution: Idempotent consumer checking record state/ID + Exponential Retry 3x -> Dead-Letter Queue `_error`)*.
   - **Multi-Tenant Security (BOLA/IDOR)**: What if user A supplies the `Guid` of a record owned by user B in the URL? *(Standard solution: Always filter repository queries by `WHERE id = @id AND owner_id = @currentUserId` extracted from validated Supabase JWT)*.
   - **Read Explosion / Database Latency**: What if a query targets 100,000 items or is invoked 500x per second? *(Standard solution: Mandatory pagination `pageSize <= 100`, composite PostgreSQL indexes, and short-TTL `HybridCache`)*.
   - **Evolution to Mobile Apps**: Do endpoints return platform-agnostic data? *(Standard solution: Pure REST JSON with OpenAPI 3.1, zero browser cookie coupling; strict `Authorization: Bearer <token>` support)*.
   - **Infrastructure Cost (MVP FinOps)**: Are unnecessary paid services being proposed? *(Standard solution: Replace with Supabase Auth Free Tier, unified PostgreSQL, In-Memory `HybridCache` L1, and containerized/CloudAMQP RabbitMQ)*.

---

## Step 2: Open Questions Resolution

1. List all ambiguous points (`Open Questions`).
2. For purely technical decisions, adopt the documented baseline in `docs/architecture/STACK_AND_DECISIONS.md` and mark as `RESOLVED`.
3. If questions involve **customer-specific business rules** that prevent accurate specification, use `ask_question` presenting clear choices with a recommended lean MVP option (`(Recommended)`).

---

## Step 3: Full Spec Generation and Atomic Task Plan

1. Create `docs/specs/<feature-slug>.md` completing **all 12 sections** of [SPEC_TEMPLATE.md](../../docs/templates/SPEC_TEMPLATE.md).
   - Avoid generic placeholders (`TODO`, `TBD`). Specify exact class names, property types, HTTP routes, status codes, RabbitMQ events, and SQL indexes.
2. Create `docs/specs/<feature-slug>-tasks.md` completing [TASKS_TEMPLATE.md](../../docs/templates/TASKS_TEMPLATE.md).
3. Strictly verify every item in [DEFINITION_OF_READY.md](../../docs/pipeline/DEFINITION_OF_READY.md). Only proceed to coding when 100% of the checklist is checked (`[x]`).

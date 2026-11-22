---
name: run-agentic-pipeline
description: >-
  Use this skill when the user asks to run the complete agentic pipeline over an idea,
  project structure, issue, or requirements. Coordinates the autonomous Software Factory V2 workflow:
  (0) Triage & Memory Ingestion -> (1) Spec-Driven Discovery & Atomic Tasks (DoR) ->
  (2) Test-Driven Implementation -> (3) Independent QA/OWASP Audit & Memory Sync -> (4) PR Delivery (DoD).
---

# Agentic Development Pipeline Execution (`run-agentic-pipeline`)

This skill orchestrates the entire software engineering lifecycle within an **Enterprise Software Factory V2**, enforcing the **4 Pillars of Spec-Driven Development (Clarity, Evidence, Independence, and Continuity)**.

---

## The End-to-End Factory V2 Workflow

```
[Trigger: Issue / Feature / Incident]
   │
   ▼
[Stage 0: Triage & Memory Ingestion] ────► Read .agents/memory/ (Patterns, Lessons, Context)
   │                                      Isolate impact (Low/Med/High) & route model tier
   ▼
[Stage 1: Spec & Atomic Tasks (DoR)] ────► Enforce Clarity (FR/NFR, Failure Modes, DDD, REST)
   │                                      Validate 100% of Definition of Ready
   ▼
[Stage 2: Implementation & Tests]    ────► TDD with Computational Guides (dotnet/npm local CPU)
   │                                      Make illegal states impossible; zero crash guards
   ▼
[Stage 3: Independent QA & DoD Gate] ────► Enforce Independence (qa-security-reviewer audit)
   │                                      OWASP Top 10, CRAP < 10, Dryer < 0.8, Mutation
   │  ┌─── Test Failure / Defect?
   └──┼───► Surgical Triage Loop back to Stage 2 with Strict Diagnostic Contract
      │
      ▼ (All passed)
[Stage 4: Memory Sync & PR Delivery] ────► Enforce Continuity: update learnedLessons.md & activeContext.md
                                          Publish PR with Factory Metrics & Verification Evidence
```

---

## Detailed Stage Protocols

### Stage 0: Triage, Diagnostics & Memory Ingestion
1. Ingest input: user prompt, bug report, or CI break.
2. Ingest Memory Bank: Read `.agents/memory/systemPatterns.md` and `.agents/memory/learnedLessons.md`.
3. If investigating a bug or CI error:
   - Run `agentic-triage-and-diagnostics` to produce the **Strict Diagnostic Contract**.
   - Classify impact (`Low | Medium | High`) and select the model tier.

### Stage 1: Discovery, Failure Mode Forecasting, and Spec (`DoR`)
1. Activate `discovery-and-spec` (or delegate to `product-requirements-analyst` and `domain-architect-planner`).
2. Resolve ambiguities. For pure technical decisions, adopt `docs/architecture/STACK_AND_DECISIONS.md`. For business ambiguities, ask the user via the interactive question tool (`AskUserQuestion` in Claude Code, `ask_question` in Gemini).
3. Produce `docs/specs/<feature>.md` and `docs/specs/<feature>-tasks.md`.
4. Validate 100% of [DEFINITION_OF_READY.md](../../docs/pipeline/DEFINITION_OF_READY.md).

### Stage 2: Test-Driven Implementation (`Gate 3`)
1. **Backend (`backend-csharp-developer`)**:
   - Execute the backend tasks listed in `docs/specs/<feature>-tasks.md` in dependency order. Typical layering: Domain + unit tests, Application (CQRS, Result Pattern, FluentValidation), Infrastructure (EF Core, Outbox, HybridCache, Supabase JWT), then Api endpoints, ArchTests and Testcontainers integration tests.
2. **Frontend (`frontend-web-developer`)**:
   - Execute the frontend tasks from the same tasks file: OpenAPI codegen, TanStack Query v5 hooks, Zod validation, UI components, Vitest/MSW tests.
3. **Run Local Computational Guides**:
   - Run `pwsh -File scripts/validate-all.ps1` (or `powershell -File` on Windows). Its exit code is the only accepted proof; never trust an agent's self-reported `tests_passed`.

### Stage 3: Independent Quality Audit & Security Gate (`DoD`)
1. Delegate verification to the independent `qa-security-reviewer` (Principle of Independence).
2. Execute `owasp-quality-gate`.
3. Validate against:
   - OWASP API Security Top 10 (BOLA/IDOR, Broken Auth, Mass Assignment, etc.).
   - Uncle Bob Disciplines: CRAP < 10, Dryer < 0.8, Mutation testing, and zero superficial crash guards.
4. *Closed-Loop Recovery*: If any check fails, trigger surgical triage, produce diagnostic contract, and route back to Stage 2. Limit to **3 fix rounds**, then escalate to the user.

### Stage 4: Memory Continuity and PR Delivery
1. Update `.agents/memory/learnedLessons.md` with newly learned solutions or framework caveats.
2. Update `.agents/memory/activeContext.md` with completed status.
3. Activate `pr-delivery-automation`:
   - Organize Conventional Commits.
   - Publish the Pull Request with Factory Metrics, architecture breakdown, and full test evidence.

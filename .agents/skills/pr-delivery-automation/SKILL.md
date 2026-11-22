---
name: pr-delivery-automation
description: >-
  Use this skill when development and tests of a feature are complete to run local computational validations,
  record Software Factory V2 metrics, update the Memory Bank (Continuity), enforce independent QA review sign-off,
  organize semantic commits, and publish a comprehensive GitHub Pull Request detailing evidence, architecture, and DoD verification.
---

# Delivery Automation and Comprehensive Pull Requests (`pr-delivery-automation`)

The goal of this skill is to ensure human or agent reviewers receive in the Pull Request **100% of context, verified test evidence, architectural decisions, security guarantees, and factory metrics**, while maintaining **Continuity** via the repository's Memory Bank.

---

## Step 1: Mandatory Pre-PR Computational Validation

1. **Run Local Computational Guides (CPU-based, zero token waste)**:
   - Run `pwsh -File scripts/validate-all.ps1` or run:
     - `dotnet build` & `dotnet format --verify-no-changes`
     - `dotnet test` (Unit, Architecture, and Testcontainers integration tests)
     - `npm run lint` & `npm test` (Vitest / React Testing Library / MSW)
2. **Capture Test Execution Evidence**:
   - Total Backend Unit, Architecture (`NetArchTest`), and Integration tests passed.
   - Total Frontend tests passed.
3. **Verify Independent Quality Gate Sign-Off**:
   - Ensure the `qa-security-reviewer` agent or `owasp-quality-gate` has formally validated the code (Principle of Independence).

4. **Evidence Honesty and Human Gate**:
   - `scripts/validate-all.ps1` exit code 0 is the only accepted proof. Exit 2 (steps `NOT RUN`) is not green; list each `NOT RUN` step in the PR as a known gap.
   - Never `git push`, open, approve, or merge the PR without an explicit user request. Prepare the report and stop.

---

## Step 2: Memory Bank Synchronization (Pillar of Continuity)

Before committing the PR:
1. **Update `learnedLessons.md`**: Record any build traps, dependency quirks, or test edge-cases encountered and solved during the task.
2. **Update `systemPatterns.md`**: If a new reusable pattern or adapter was introduced, record it.
3. **Update `activeContext.md`**: Mark the feature as completed, link the PR, and outline the next items.

---

## Step 3: Branch and Commit Standardization (Conventional Commits)

- **Branch Naming**: `feat/<feature-slug>`, `fix/<bug-slug>`, `chore/<slug>`
- **Commit Formatting**:
  - `feat(domain): add Order aggregate and value objects`
  - `feat(api): expose POST /api/v1/orders minimal api endpoint`
  - `feat(web): implement order creation form with TanStack Query and Zod`
  - `test(integration): add Postgres and RabbitMQ outbox integration tests`
  - `docs(memory): update learned lessons and active context for order feature`

---

## Step 4: Mandatory Pull Request Structure with Factory Metrics

When creating the Pull Request (via GitHub MCP or CLI), **mandatorily** fill out:
1. **Executive Summary and Spec Link** (`docs/specs/<feature>.md`).
2. **Software Factory V2 Metrics**:
   - *Impact Level*: `Low | Medium | High`
   - *Atomic Tasks Completed*: Number of tasks from the spec plan.
   - *Computational Evidence*: Summary of local CPU checks executed.
   - *Model Tier Used*: Rationale for model selection.
3. **Architecture and Layered Changes** (`Domain`, `Application`, `Infrastructure`, `Api`, `Frontend`).
4. **Foreseen Failure Modes & Implemented Mitigations** (Concurrency, Outbox, Supabase BOLA protection, Cache).
5. **Real Executed Test Report** (Count of passing tests, coverage evidence, no crash guards).
6. **OWASP API Top 10 & Definition of Done (DoD) Checklist**.
7. **Deployment & Rollback Guide** (EF Core migrations, environment variables).

---
name: run-agentic-pipeline
description: >-
  Use this skill when the user asks to run the complete agentic pipeline over an idea,
  project structure, or requirements. Coordinates the autonomous multi-stage workflow:
  (1) Discovery, FR/NFR, Risks, and Spec -> (2) DDD/Hexagonal Architecture and Tasks (DoR) ->
  (3) Backend C# and Frontend Implementation -> (4) QA/OWASP Audit and Pull Request (DoD).
---

# Agentic Development Pipeline Execution (`run-agentic-pipeline`)

This skill orchestrates the entire software engineering lifecycle in an autonomous, reactive, and standardized manner.

---

## Executing the End-to-End Pipeline

### Stage 1: Structure Bootstrap (If codebase does not exist yet)
If the project folder contains only business requirements or an initial briefing and lacks the `src/backend` and `src/frontend` scaffold:
1. Run the baseline architecture scaffold script (or structure manually following `docs/architecture/STACK_AND_DECISIONS.md`):
   ```powershell
   pwsh -File scripts/scaffold-monolith.ps1
   ```

### Stage 2: Discovery, Problem Forecasting, and Spec (`Gate 1 & Gate 2 - DoR`)
1. Activate the `discovery-and-spec` skill (or delegate to `product-requirements-analyst` and `domain-architect-planner`).
2. Deeply analyze all context provided in the workspace.
3. Identify **Open Questions**. If genuine business blockers arise, ask the user objectively before coding.
4. Generate:
   - `docs/specs/<feature>.md` (Full specification with FR, NFR, Foreseen Failure Modes + Solutions, DDD, REST Contracts, RabbitMQ Events, Caching, and SQL).
   - `docs/specs/<feature>-tasks.md` (Sequential atomic tasks with verification commands).
5. Validate 100% of [DEFINITION_OF_READY.md](../../docs/pipeline/DEFINITION_OF_READY.md).

### Stage 3: Test-Driven Sequential/Parallel Implementation (`Gate 3`)
With the OpenAPI REST contracts and Spec frozen in Stage 2:
1. **Backend (`backend-csharp-developer`)**:
   - Executes `TASK-01` (Domain + Unit Tests) -> `TASK-02` (Application + Validators + Unit Tests) -> `TASK-03` (Infra EF Core Postgres + MassTransit RabbitMQ Outbox + HybridCache + Supabase Auth) -> `TASK-04` (Minimal API + ArchTests + IntegrationTests).
2. **Frontend (`frontend-web-developer`)**:
   - Executes `TASK-05` (OpenAPI type synchronization, HTTP Client with Supabase Auth, TanStack Query Hooks, UI Components + Zod Forms + Vitest/MSW Tests).

### Stage 4: Rigorous Quality Audit, OWASP, and Pull Request (`Gate 4 - DoD`)
1. Delegate to `qa-security-reviewer` (or execute `owasp-quality-gate` + `pr-delivery-automation`).
2. Run `pwsh -File scripts/validate-all.ps1`.
3. If any compilation error, linter warning, test failure, or OWASP vulnerability emerges, fix immediately and re-run until achieving **100% pass rate**.
4. Generate the final Pull Request review artifact detailing all changes and executed test results.

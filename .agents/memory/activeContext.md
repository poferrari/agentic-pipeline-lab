# Active Workspace Context

**Last Updated**: 2026-11-22
**Status**: Pull Request being prepared on branch `chore/upgrade-nextjs-16` consolidating Next.js 16/React 19 upgrades, enterprise agentic pipeline, memory bank, safety hooks, and quality gates.

---

## 1. Current Strategic Focus
- Turn prose rules into machine-verified gates: `scripts/validate-all.ps1` (PASS/FAIL/NOT RUN), CRAP ratchet, jscpd duplication, Stryker.NET (opt-in), EN-US check.
- Keep one source of rules (`AGENTS.md`); `CLAUDE.md`, `GEMINI.md`, and the plugin rules file only point to it.

## 2. Completed in This Hardening Pass
- [x] Agents: role-specific `model` and minimal `tools` (QA is read-only); orchestrator hard gates and 3-round fix loop.
- [x] `pipeline_workflow.py`: Ambiguity and DoR gates enforced, gate runner uses the real exit code, backend/frontend in parallel, QA feedback loop.
- [x] Hooks: shared `safety-policy.ps1` (Claude + Gemini), Write/Edit secret guard, PostToolUse format, Stop hook tied to the gate marker.
- [x] CI: measured summaries, Trivy pinned and failing on CRITICAL/HIGH, Gitleaks on the whole repo, governance job, CRAP step.
- [x] Skills `csharp-hexagonal-ddd` and `software-architecture-patterns` split into `SKILL.md` + `references/`.

## 3. Open Issues (not caused by the pipeline work)
- `App.IntegrationTests`: 2 tests failed (`ExponentialRetry_...`, `TransactionalOutbox_WhenTransactionNotCommitted_...`). ROOT CAUSE (verified 2026-11-19): the Docker daemon was not running, so `MarketPulseTestcontainersFactory` silently fell back to in-memory (`IsDockerEnvironmentActive=False`, MassTransit started on `loopback://`, no RabbitMQ, no Postgres, no outbox, no `UseMessageRetry`). The other 20 integration tests passed vacuously on that fallback. The app code is NOT proven faulty. FIX APPLIED (TDD): `MarketPulseTestcontainersFactory` now fails loudly via `InfrastructureFallbackPolicy` (opt-in `MARKETPULSE_ALLOW_INMEMORY_FALLBACK=true`), and `RealInfrastructureTests` asserts Npgsql + a `rabbitmq://` bus. Without Docker, 23 integration tests fail with a clear message instead of 20 passing vacuously.
- 19 methods exceed CRAP 10 (see `scripts/quality/crap-baseline.json`); worst: `OpenAiChatAdvisorAdapter`, `Program.ResolveClientPartitionKey`, `FrankfurterExchangeAdapter`.
- OpenAPI sync check (`schema.d.ts` vs backend `openapi/v1.json`) is not automated; DoD item still manual.
- Unvalidated here: Gitleaks on the whole repo and Trivy fail-on-HIGH in CI (no run possible locally). Expect to tune `.gitleaks.toml` / `.trivyignore` on the first CI run.
- `npm audit --omit=dev --audit-level=high` fails on `src/frontend`: Next.js 14 (critical advisories, fix is a breaking upgrade to 16.x) and postcss <= 8.5.22. The old CI hid this with `|| true`; the frontend CI now fails until Next.js is upgraded.
- `conhecimento/` is git-ignored local research; do not reference it from tracked docs.

## 4. Next Steps
- Fix the 2 failing integration tests, then run `validate-all.ps1` (full) and refresh the CRAP baseline downward.
- Raise the Stryker `break` threshold (now 45) as the mutation score improves; first measured score on Domain + Application with unit tests was 46.89% (226 mutants tested, 2026-11-19). Target: high 80 / low 60.
- Open a PR for the hardening branch only on explicit user request.

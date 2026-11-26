# Active Workspace Context

**Last Updated**: 2026-11-26
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

## 2b. Completed: Test-Design Hardening (2026-11-26)
- [x] `testing-battery.md` rewritten: levels, equivalence/boundary design, naming, doubles policy, ambient inputs, flakiness, AI testing pyramid.
- [x] DoR/DoD, Spec and Tasks templates, `systemPatterns.md` (cross-layer checklist, explicit non-decisions), QA reviewer (test quality audit, change surface review), backend/frontend agents updated.
- [x] Domain and Application no longer read the system clock: `TimeProvider` injected (handlers), timestamps required by `CurrencyWatchItem.Create`/`EvaluateMarketRate`. Tests use `FakeTimeProvider`.
- [x] `scripts/quality/check_test_hygiene.py` (+ unit tests, wired into `validate-all.ps1` and CI): ambient clock in Domain/Application and a ratchet on fixed waits in tests.
- [x] Advisor adversarial corpus added; fixed a real bypass (hostile requests borrowing "rate"/"currency" skipped the refusal).
- [x] `pipeline_workflow.py`: stops on a repeated identical failure and caps agent calls (syntax-checked only; needs a real run to verify).

## 2c. Completed: Ingestion & Governance from 4 Casa do Codigo Books (2026-11-26)
- [x] Converted 4 books with `markitdown` (Claude Code .NET 9, Engenharia de IA/MCP, Arquitetura Hexagonal & DDD, Engenharia de Prompt 2nd Ed).
- [x] Added 3 operational playbooks in `docs/templates/`: `DEBUG_PLAYBOOK_TEMPLATE.md` (symptom-first, zero speculative fixes), `REFACTOR_PLAYBOOK_TEMPLATE.md` (green baseline, what changes vs what does not, unified diffs), `TEST_PLAYBOOK_TEMPLATE.md` (isolated session test generation, anti-confirmation bias).
- [x] Enriched `csharp-hexagonal-ddd`: canonical `readonly record struct` Value Objects, domain-level ownership authorization (`IsOwnedBy` against BOLA/IDOR), explicit prohibition of generic repositories (`IRepository<T>`) and `IQueryable` leakage, and state machine transition tables.
- [x] Enriched `sre-observability-telemetry`: OpenTelemetry GenAI semantic conventions (`gen_ai.client.token.usage`, `gen_ai.request.duration`, TTFT, TPOT, cost estimation, and ReAct loop spans).
- [x] Added Anti-Cialdini (Anti-Persuasion) and Empirical Debugging rules to `AGENTS.md`.
- [x] Implemented `scripts/quality/check_prompt_safety.py` (30 adversarial injection vectors from Larguesa's red-teaming catalog) with 13 passing unit tests in `scripts/quality/tests/` wired into `scripts/validate-all.ps1`.
- [x] Recorded Lessons 20, 21, and 22 in `learnedLessons.md`.

## 3. Open Issues (not caused by the pipeline work)
- The CRAP baseline key for `CurrencyWatchItem::Create` was renamed by hand for the new signature (score unchanged); confirm it with the full gate result.
- `RabbitMqMessagingIntegrationTests`: the end-to-end test now proves consumption by polling (fixed); two bounded negative-proof waits remain (baseline in `test-hygiene-baseline.json` is 3 waits for that file, including the polling helper).
- Golden set for the live-provider path of the advisor is not built (only the deterministic engine has a corpus).
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

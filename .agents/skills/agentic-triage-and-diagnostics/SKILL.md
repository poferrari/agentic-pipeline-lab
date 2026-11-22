---
name: agentic-triage-and-diagnostics
description: Autonomous incident and issue triage with strict structured contracts. Isolates root causes, assesses impact (Low/Medium/High), routes tasks to appropriate model tiers, and prepares reproduction test specifications before code generation.
---

# Agentic Triage and Diagnostics Skill (`agentic-triage-and-diagnostics`)

## 1. Overview and Intent

In an enterprise-grade **Software Factory V2**, issues, CI test breakages, production error logs (Sentry/OpenTelemetry), and feature requests must not be dumped into code generation agents as raw text or bloated prompts.

This skill provides a **deterministic triage protocol**:
1. **Surgical Context Isolation**: Extracts exact stack traces, affected files, and boundary conditions without bloating the context window with the entire repository.
2. **Impact Assessment & Model Routing**: Classifies impact to select the optimal model tier (saving costs and token budgets).
3. **Strict Data Contract**: Emits a strongly-typed JSON/Markdown diagnostic output rather than conversational prose.

---

## 2. Impact Assessment Matrix & Model Routing

| Impact Level | Criteria | Recommended Action & Model Tier |
| :--- | :--- | :--- |
| **Low** | Typo, localized UI styling, documentation, minor test flake, single-file forward-compatible tweak. | Use fast/lightweight model (`flash_lite` or default). Immediate fix without full architectural redesign. |
| **Medium** | Isolated logic bug, edge-case failure, new CRUD endpoint, query optimization without schema changes. | Use balanced standard model (`inherit` / `flash`). Requires reproduction unit test and Result Pattern handling. |
| **High / Critical** | Schema migration, breaking API contract change, security vulnerability (OWASP Top 10), distributed race condition, transactional outbox failure. | Requires deep reasoning model (`pro` / high reasoning effort). Full Spec update, ADR, and independent QA/OWASP audit required. |

---

## 3. The Strict Diagnostic Contract

The triage step must return the following structured diagnosis artifact:

```json
{
  "triage_id": "TRG-YYYYMMDD-01",
  "source": "CI_FAILURE | SENTRY_ALERT | USER_ISSUE | SYSTEM_LOG",
  "impact_level": "LOW | MEDIUM | HIGH",
  "model_tier_recommended": "flash_lite | inherit | pro",
  "root_cause_hypothesis": "Concise statement of why the failure occurred at origin (anti-crash-guard: no superficial null-hiding).",
  "affected_components": [
    {
      "file_path": "path/to/file.ext",
      "function_or_class": "NameOfMethodOrClass",
      "expected_behavior": "...",
      "actual_behavior": "..."
    }
  ],
  "reproduction_test": {
    "test_file": "tests/path/to/Test.cs",
    "test_name": "Should_FailGracefully_When_BoundaryConditionTriggered",
    "test_type": "UNIT | INTEGRATION | ARCHITECTURE"
  },
  "constraints_and_security": [
    "No breaking changes to public contracts",
    "Preserve tenant isolation and Supabase JWT checks",
    "Keep CRAP < 10 and maintain zero crash-guard policy"
  ]
}
```

---

## 4. Operational Execution Steps

1. **Step 1: Ingest Failure Trace**:
   - Parse error stack trace or bug report.
   - Run local computational diagnostics (e.g., `dotnet test --filter <FailedTest>` or `npm test`).
2. **Step 2: Inspect Affected Files Surgically**:
   - Read ONLY the files referenced in the trace (do not read unrelated project directories).
   - Check `.agents/memory/learnedLessons.md` to see if this failure mode is known.
3. **Step 3: Produce the Diagnostic Artifact**:
   - Write or return the Strict Diagnostic Contract.
4. **Step 4: Dispatch to Implementation Agent**:
   - Provide the implementation agent (`backend-csharp-developer` or `frontend-web-developer`) with the diagnosis artifact as its primary input.


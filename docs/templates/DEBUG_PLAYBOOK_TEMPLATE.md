# Debug Playbook: Empirical Incident Resolution

> **Golden Rule**: Never prescribe a preconceived solution or speculative fix in the initial prompt or triage.
> Describe exclusively the observed symptoms, expected behavior, and reproduction steps.
> LLMs exhibit confirmation bias: prescribing a solution forces the agent to patch symptoms with superficial crash guards (`if (x != null)`) instead of finding the true root cause.

---

## 1. Incident Metadata

| Attribute | Details |
| :--- | :--- |
| **Incident / Bug ID** | `BUG-YYYYMMDD-XXX` |
| **Component / Service** | `[e.g., Backend C# Minimal API / Currency Consumer / Supabase Auth]` |
| **Severity (Impact)** | `[Low / Medium / High / Critical]` |
| **Date & Environment** | `[e.g., 2026-11-26, Local / Staging / Production]` |
| **Reported By** | `[User / Automated Alert / QA]` |

---

## 2. Observed Symptom vs. Expected Behavior

### 2.1. Observed Symptom
- **What happened**: *[Describe exact failure, exception type, error code, or missing UI/API data]*
- **Observed output / logs**:
  ```text
  [Insert raw error log or ProblemDetails response without speculation]
  ```

### 2.2. Expected Behavior
- **What should have happened**: *[Describe valid contract behavior, expected HTTP status, or domain state]*

---

## 3. Deterministic Reproduction Steps

1. **Prerequisites**: *[Seed data, auth token, background queue state]*
2. **Action / Trigger**:
   ```bash
   # Exact curl, dotnet test filter, or HTTP payload triggering the bug
   curl -X POST http://localhost:5000/api/v1/resource -H "Content-Type: application/json" -d '{...}'
   ```
3. **Reproducibility Rate**: `[100% / Flaky (describe concurrency or race condition)]`

---

## 4. Empirical Investigation & Root Cause Analysis (RCA)

*(To be filled during agent investigation)*

- **Where the Illegal State Originated**: *[File and line where invalid data was created or unvalidated]*
- **Why it Passed Existing Tests**: *[Gap in boundary condition, missing test scenario]*
- **Hypothesis Tested**:
  1. Hypothesis A: *[State assumption and test result]*
  2. Confirmed Root Cause: *[Single sentence explaining the real origin]*

---

## 5. Automated Regression Test Specification

- **Failing Test Name**: `Given_[Scenario]_When_[Action]_Then_[ExpectedFix]`
- **Test File Location**: `tests/App.UnitTests/...` or `tests/App.IntegrationTests/...`
- **Verification Criterion**: The test **MUST FAIL** before any production code change, and **MUST PASS** once the fix is applied.

---

## 6. Binary Acceptance Criteria (Uncle Bob Disciplines)

- [ ] **Root Cause Fixed**: Zero superficial crash guards (`if (x != null)`, blank `catch`) added.
- [ ] **Regression Test Added**: Automated test reproduces the failure and is green.
- [ ] **Deterministic Quality**: CRAP score < 10 for all modified methods.
- [ ] **Zero Regressions**: `scripts/validate-all.ps1 -Fast` passes with exit code 0.
- [ ] **Learned Lesson Documented**: Post-mortem recorded in `.agents/memory/learnedLessons.md`.

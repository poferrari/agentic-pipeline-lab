# Refactor Playbook: Behavior-Preserving Structural Improvement

> **Golden Rule (Martin Fowler & Daniel Portugal)**: Refactoring is changing internal code structure without altering observable external behavior.
> A green test baseline is mandatory before touching any code.

---

## 1. Refactoring Scope & Motivation

| Attribute | Details |
| :--- | :--- |
| **Target Area / Class** | `[e.g., App.Application.Orders.Handlers.CreateOrderHandler]` |
| **Motivation / Code Smell** | `[e.g., High CRAP score (> 10), Structural Duplication (Dryer), Deep Nesting, Primitive Obsession]` |
| **Target Architecture** | `[Hexagonal Ports & Adapters / DDD Value Object / Clean Minimal API]` |

---

## 2. Mandatory Pre-Refactoring Baseline

Before issuing any refactoring prompt or modifying a single line of code, the automated test suite must run and pass completely:

```powershell
# Mandatory baseline check (must exit 0)
./scripts/validate-all.ps1 -Fast
```

- [ ] Baseline confirmed green at commit `[insert git commit hash]`.
- [ ] Current test coverage covers the target component.

---

## 3. Explicit Refactoring Boundaries

### 3.1. What Changes (Internal Structure)
- *[Extract private method, replace primitive with Value Object, extract Outbox dispatcher, etc.]*
- *[Reduce cyclomatic complexity under 10]*
- *[Eliminate duplicate logic]*

### 3.2. What Does NOT Change (Invariants & Contracts)
- ❌ **Public API Contracts**: Request/Response DTOs, JSON schemas, route names, HTTP status codes remain identical.
- ❌ **Database Schemas / Migrations**: Table columns, constraints, and data layouts remain untouched.
- ❌ **External Events**: Message broker payloads and event contract names remain identical.
- ❌ **Domain Invariants**: Business rules, error codes, and validation conditions are strictly preserved.

---

## 4. Delivery Format: Unified Diff (`diff -u`)

For modifications to files larger than 50 lines, agents must deliver changes as surgical unified diffs or targeted replacements, avoiding full-file rewrites that risk dropping subtle logic:

```diff
--- a/src/backend/src/App.Application/Services/CalculationService.cs
+++ b/src/backend/src/App.Application/Services/CalculationService.cs
@@ -14,8 +14,5 @@
-    // Old complex nested logic
+    // Extracted readable helper
```

---

## 5. Post-Refactoring Quality Gates

- [ ] **Deterministic Quality Gate**: Every modified method has **CRAP < 10**.
- [ ] **Duplication Gate**: Code duplication index remains **Dryer < 0.8** (`jscpd`).
- [ ] **Mutation Resilience**: Behavioral mutation tests pass (`dotnet stryker`).
- [ ] **Full Validation**: `./scripts/validate-all.ps1` runs clean with exit code 0.

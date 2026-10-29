---
name: owasp-quality-gate
description: >-
  Use this skill prior to completing any implementation or during code review to audit
  security (OWASP Top 10 and OWASP API Security Top 10), Clean Code, KISS, Hexagonal Architecture,
  test coverage, and validate the Definition of Done (DoD).
---

# Quality Audit and OWASP Security Gate (`owasp-quality-gate`)

This skill acts as the **Automated Quality Gate** of the pipeline. Execute all checks below before opening or approving any Pull Request.

---

## 1. OWASP API Security Top 10 Checklist (Codebase Verification)

| OWASP Code | Vulnerability | Mandatory Check in Backend C# and Frontend TS |
| :--- | :--- | :--- |
| **API1:2023** | **Broken Object Level Authorization (BOLA / IDOR)** | Does every route with `{id}` (`/api/v1/orders/{id}`) pass `currentUser.Id` (from Supabase JWT) to repository queries? Is it impossible to read/mutate other users' data by tampering with the URL UUID? |
| **API2:2023** | **Broken Authentication** | Does `JwtBearer` validate `Issuer`, `Audience`, `Lifetime`, and Supabase JWKS asymmetric signature? Does Frontend use `@supabase/ssr` without leaking tokens in URLs? |
| **API3:2023** | **Broken Object Property Level Authorization (Mass Assignment)** | Do Minimal API endpoints accept dedicated `sealed record` Request DTOs? Are domain entities and EF Core models completely hidden from HTTP parameter binding? |
| **API4:2023** | **Unrestricted Resource Consumption** | Do all listing queries enforce strict page size limits (e.g. `RuleFor(x => x.PageSize).InclusiveBetween(1, 100)`)? Are partitioned Rate Limiting (`UseRateLimiter`) and max request body size (2MB) active? |
| **API5:2023** | **Broken Function Level Authorization** | Do all endpoint groups enforce `.RequireAuthorization()` by default, unless explicitly documented and publicly intended? |
| **API6:2023** | **Unrestricted Access to Sensitive Business Flows** | Do critical state-changing workflows (payments, bookings, creations) require `Idempotency-Key` headers and anti-abuse safeguards? |
| **API7:2023** | **Server-Side Request Forgery (SSRF)** | If the backend performs external HTTP calls based on inputs, does the URL pass through a strict domain allowlist with private/loopback IP blocking? |
| **API8:2023** | **Security Misconfiguration** | Is `CORS` restricted to explicitly configured origins (no wildcard `AllowAnyOrigin()` with credentials)? Does `ProblemDetails` strip stack traces when `IsProduction()`? Are OWASP Security Headers (`CSP`, `X-Content-Type-Options`, `HSTS`) active? |
| **API9:2023** | **Improper Inventory Management** | Are routes strictly versioned (`/api/v1/...`) and documented in OpenAPI? Are debug/testing endpoints disabled in production? |
| **API10:2023** | **Unsafe Consumption of APIs** | Do external integrations and RabbitMQ consumers validate inbound payloads, enforcing `Timeout`, `Retry`, and `CircuitBreaker` (`Microsoft.Extensions.Http.Resilience`)? |

---

## 2. Clean Code, KISS, and Hexagonal Architecture Checklist

1. **Zero Prohibited Dependencies**: `App.Domain` has zero infrastructure `using` statements.
2. **No Exceptions for Business Flow**: Handlers return explicit `Result<T>` values.
3. **No Tracked Secrets in Git**: Verify with `git status` and `git diff` that no `.env` file, database credentials, or private keys are staged.
4. **Secure and Performant Queries**: Zero string concatenation in SQL (complete SQL Injection prevention via parameterized EF Core / Dapper), and mandatory `.AsNoTracking()` on read queries.

---

## 3. Unified Validation Script Execution

Always execute the unified pipeline validation script to ensure compilation, linting, unit, architecture, and integration tests succeed:

```powershell
pwsh -File scripts/validate-all.ps1
```
If any check fails, resolve the root cause prior to proceeding.

---

## 4. Defense-in-Depth and Automated Pentest Battery

For edge perimeter defense architecture, origin bypass mitigation on Render, and automated defensive tests, consult:
* Master guide: `docs/security/DEFENSE_IN_DEPTH_AND_HARDENING_GUIDE.md`
* Automated security test suite: `src/backend/tests/App.IntegrationTests/Security/SecurityHardeningAndPentestVerificationTests.cs`

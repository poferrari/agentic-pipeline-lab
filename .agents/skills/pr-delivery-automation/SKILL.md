---
name: pr-delivery-automation
description: >-
  Use this skill when development and tests of a feature are complete to
  run the final validation test suite, organize semantic commits (Conventional Commits), and
  generate or publish a comprehensive GitHub Pull Request detailing architecture, executed test evidence,
  OWASP security audits, and Definition of Done (DoD) verification.
---

# Delivery Automation and Comprehensive Pull Requests (`pr-delivery-automation`)

The goal of this skill is to ensure human or agent reviewers receive in the Pull Request **100% of context, verified test evidence, architectural decisions, and security guarantees**, making review fast, transparent, and confident.

---

## Step 1: Mandatory Pre-PR Validation

1. Run `pwsh -File scripts/validate-all.ps1` (or individual build, lint, and test commands for backend and frontend).
2. Capture the exact test execution evidence:
   - Count of **Backend Unit Tests** executed and passed.
   - Count of **Architecture Tests (`NetArchTest`)** executed and passed.
   - Count of **Backend Integration Tests (`Testcontainers`)** executed and passed.
   - Count of **Frontend Tests (`Vitest / MSW / Playwright`)** executed and passed.
3. Verify `git status` and `git diff` to ensure no temporary files, logs, or secrets (`.env`) are staged.

---

## Step 2: Branch and Commit Standardization (Conventional Commits)

- **Branch Naming**: `feat/<feature-slug>`, `fix/<bug-slug>`, `chore/<slug>`
- **Commit Formatting**:
  - `feat(domain): add Order aggregate and value objects`
  - `feat(api): expose POST /api/v1/orders minimal api endpoint`
  - `feat(web): implement order creation form with TanStack Query and Zod`
  - `test(integration): add Postgres and RabbitMQ outbox integration tests`

---

## Step 3: Mandatory Pull Request Description Structure

When creating the Pull Request (via GitHub MCP or CLI), **mandatorily** fill out all sections of `.github/PULL_REQUEST_TEMPLATE.md`:

1. **Executive Summary and Link to Spec** (`docs/specs/<feature>.md`).
2. **Architecture and Layered Changes** (`Domain`, `Application`, `Infrastructure`, `Minimal API`, `Frontend`).
3. **Foreseen Failure Modes & Implemented Mitigations** (e.g. how RabbitMQ idempotency, Supabase BOLA protection, and caching were handled).
4. **Real Executed Test Report** (Table detailing total passed, failed, skipped, and duration).
5. **OWASP Top 10 Security Checklist** and **Definition of Done (DoD)** sign-off.
6. **Deployment, Database Migrations, and Rollback Guide** (EF Core migrations or required environment variables).

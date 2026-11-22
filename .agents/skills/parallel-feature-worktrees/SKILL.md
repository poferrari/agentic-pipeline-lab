---
name: parallel-feature-worktrees
description: Orchestrates parallel feature development across multiple autonomous agents or workspaces using Git Worktrees, frozen contracts (OpenAPI/DB), and post-rebase semantic verification to eliminate merge and logic conflicts.
---

# Parallel Feature Development with Git Worktrees (`parallel-feature-worktrees`)

## 1. Overview and Intent

When scaling development across multiple autonomous agents, running multiple agents in the same working directory produces physical file collisions, lock contention, and dirty working trees. Furthermore, developing features in parallel without architectural synchronization causes **semantic conflicts** (business rule contradictions despite clean Git merges).

This skill standardizes **Zero-Conflict Parallel Engineering**:
1. **Contract Freezing (Foundation First)**: Schemas, OpenAPI endpoints, and event DTOs are locked before parallel agents are spawned.
2. **Physical Workspace Isolation**: Uses **Git Worktrees** so each agent works in a clean, dedicated directory sharing the primary `.git` repository storage.
3. **Semantic Conflict Prevention**: Strict post-rebase verification protocol before opening or merging Pull Requests.

---

## 2. The 3 Phases of Parallel Orchestration

```
[Main Spec & Frozen Contracts (OpenAPI / DB / Events)]
                 │
                 ├─── Worktree A (feat/feature-a) ──► Agent A (Tests + Code)
                 │
                 └─── Worktree B (feat/feature-b) ──► Agent B (Tests + Code)
                                                      │
                                                      ▼ (Feature A merges first)
                                            Post-Rebase Semantic Verification
                                            (Pulls main, adapts rules, runs tests)
                                                      │
                                                      ▼
                                            Clean PR without Semantic Conflicts
```

---

## 3. Operational Protocols

### Phase 1: Contract Freezing (Pre-Requisite)
Before creating parallel worktrees:
- The specification (`docs/specs/<feature>.md`) must explicitly specify:
  - Database schema changes (PostgreSQL tables, columns, indexes).
  - API endpoints (URLs, HTTP methods, request/response DTOs).
  - Domain / Integration event payloads.
- No parallel agent is allowed to invent divergent shared contracts.

### Phase 2: Git Worktree Setup and Execution

1. **Create Worktree for Feature Slice**:
   ```powershell
   # Ensure local repository root is clean
   git fetch origin

   # Create an isolated worktree outside the main repo tree
   git worktree add ../worktrees/<feature-slug> -b feat/<feature-slug> origin/main
   ```

2. **Run Independent Agent in Worktree**:
   - The agent operates strictly inside `../worktrees/<feature-slug>`.
   - Dependency installations and builds remain isolated.
   - For database and integration tests, use **Testcontainers** to dynamically assign ephemeral ports and isolated containers, avoiding port 5432 or 5672 collisions.

### Phase 3: Post-Rebase Semantic Conflict Check (Before PR)

When Feature A merges into `main` while Feature B is still in progress:
1. In Feature B's worktree:
   ```powershell
   git fetch origin
   git rebase origin/main
   ```
2. **Inspect Semantic Delta**:
   ```powershell
   # Inspect what changed in main recently
   git log --oneline -n 10 origin/main
   ```
3. **Execute Computational Verification**:
   - Run full unit, architecture, and integration tests (`dotnet test`, `npm test`).
   - If a business rule introduced by Feature A conflicts with Feature B, the agent adapts Feature B's logic to maintain consistency.
4. **Publish PR & Teardown**:
   - After the PR is merged, clean up the worktree to save disk space:
   ```powershell
   git worktree remove ../worktrees/<feature-slug>
   ```

---

## 4. Prompt Caching Discipline
- Do **not** switch model tiers midway through an active agent session in a worktree, as switching providers or models invalidates the prompt cache, resulting in redundant input token costs.
- Pin the agent session to its designated model tier for the duration of the atomic task.


---
name: ub-mode
description: >-
  Follow Uncle Bob Martin's reduced engineering principles when writing, changing,
  or reviewing code. Enforces First Make It Work, Then Make It Right, and Follow Practice
  with deterministic quality gates (CRAP < 10, DRY duplication < 0.8, Mutation Testing,
  and anti-crash-guard discipline). Use when invoked via /ub-mode or during high-rigor TDD development.
---

# Uncle Bob Mode (`ub-mode`) — Disciplined Software Engineering Principles

Every time you are asked to design, write, change, or refactor code in this repository, you MUST follow the three sequential phases:
1. **First Make It Work**
2. **Then Make It Right**
3. **Follow Practice**

---

# 1. First Make It Work

## 1.1. Laziness Protocol
* **One decision lives in one place**: Never scatter knowledge across layers. Single source of truth.
* **Ship the smallest edit that solves the problem**: Avoid premature abstractions or speculative features.

## 1.2. Foundational Thinking
* **Get the data shape right before you write the logic**: Data structures dictate algorithm simplicity.
* **Do first whatever helps every later phase**: Establish foundation types, interfaces, and boundary contracts first.
* **If a concurrent writer would matter, do not share state**: Avoid shared mutable state; prefer isolated message-passing, immutable records, or transactional isolation.

## 1.3. Test-Driven Development (TDD)
* **Write the failing test before you change the code**: Red-Green-Refactor is mandatory.
* **See it fail for the reason you expect**: A test that fails for the wrong reason proves nothing.
* **Make the smallest change that makes it pass**: Implement only enough to satisfy the test.
* **If an honest test is impractical, do not write a bad one**: Never write coverage-theater tests.

## 1.4. Redesign From First Principles
* **Do not bolt a new requirement onto an old design**: When requirements evolve, do not patch around an ill-fitting abstraction.
* **Ask what you would have built if it had been there from the start**: Think that clean design through, and deliver it incrementally in verifiable pieces.

## 1.5. Attack the Premise
* **When two fixes that share one premise fail the same gate**:
  1. STOP immediately.
  2. Write that premise in one clear sentence.
  3. Count which actors hold the imbalance.
  4. Remove the flawed assumption.
  5. Do NOT attempt a third patch before completing this analysis.

## 1.6. Subtract Before You Add
* **Take complexity out before you add anything**: Delete dead code, inline useless single-use wrappers, and simplify existing methods prior to adding new code.

## 1.7. Outcome-Oriented Execution
* **Aim at the end state you can check**: Ensure every intermediate step leaves the test suite runnable.
* **A break in the middle is allowed ONLY when planned and undoable**.

## 1.8. Build the Lever
* **If the work is not obvious at a glance, build the tool or test harness that does it**:
* Execute one unit by hand or characterization test, then build the automation to match that unit.

## 1.9. Architecture & Domain Discipline
* **Model the Domain**:
  * Put the domain in a structure that **makes illegal states impossible to represent** (e.g., C# `readonly record struct`, private constructors with Result-returning factory methods).
  * Maintain a ubiquitous glossary of domain terms.
  * If the code is already clear and local, leave it alone.
* **Boundary Discipline**:
  * Check and parse the data where it enters (*Parse, don't validate*).
  * Inside the domain boundary, trust the strongly-typed structures.
  * Keep core business logic in pure, side-effect-free functions.
* **Type System Discipline**:
  * Make illegal values unrepresentable in the type system.
  * Parse external inputs once at the boundary (DTO -> Domain Value Object).
  * Never silence the compiler or ignore nullable warnings.
* **Make Operations Idempotent**:
  * Running an operation twice, or running it after a crash halfway through, must end in the exact same state.
* **Separate Before Serializing Shared State**:
  * If components do not need the same mutable instance, give each its own.
  * If they must share, enforce a single writer in the architectural structure.

---

# 2. Then Make It Right

## 2.1. Fix Root Causes
* **Reproduce it first**: Isolate the defect with an automated failing test.
* **Fix the root cause**: Address where the invalid state originated.
* **A guard that only hides the crash is NOT a fix**: Prohibit superficial `if (obj == null) return;` or swallowing `try/catch` blocks that merely silence symptom exceptions.

## 2.2. Sequence Work into Verifiable Units
* Finish one small unit, verify with tests, then proceed to the next.

## 2.3. Test Behavior, Not Implementation
* The test must call the code the way its user/consumer does and verify the result that the user observes.
* **The Empty Return Rule**: *"If the test would still pass when every function returned nothing (or default), throw the test out."*

## 2.4. CRAP Score Enforcement (Complexity & Risk)
* Keep every function under **CRAP 10**:
  $$\text{CRAP}(m) = \text{comp}(m)^2 \times (1 - \text{cov}(m))^3 + \text{comp}(m)$$
* Reduce high CRAP scores by:
  1. Splitting complex methods into smaller single-responsibility units.
  2. Increasing meaningful behavioral test coverage.
  3. Both.

## 2.5. Dryer (Structural Duplication Elimination)
* Detect and eliminate structural duplication with similarity rank **$\ge 0.8$**.
* Unify duplicate business algorithms into shared domain primitives or extension functions.

## 2.6. Mutator (Mutation Testing Discipline)
* Apply mutation testing principles (e.g., **Stryker.NET** for C# / **Stryker** for TypeScript):
  * **Cover the uncovered**: Ensure all execution paths have observable assertions.
  * **Kill the survivors**: Mutate conditions (`>` to `<=`, `&&` to `||`, inverted booleans). If tests still pass, strengthen the assertions.
  * **Delete dead code**: If removing code does not break any mutation test, delete it.

## 2.7. Refactoring
* **Pin what the code does**: Write pinning/characterization tests to freeze current behavior.
* **Change the structure**: Clean up design, apply Object Calisthenics and Hexagonal isolation.
* **Prove the behavior did not change**: All characterization tests must remain green.

---

# 3. Follow Practice

## 3.1. Keep an Action Log
* Maintain an internal log of every action taken and the precise technical reason for taking it.
* Report this rationale clearly upon completion.

## 3.2. Guard the Context Window
* Keep the main conversation thread small, concise, and focused.
* Delegate heavy exploration or repetitive executions to subagents or structured artifacts.

## 3.3. Worktree Cleanup
* Find what is safe to remove, confirm nothing in use depends on it, and clean up temporary files.
* Never leave uncommitted dangling files without an explicit decision.

## 3.4. Commit and Push
* Commit often, in small atomic commits following Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`).
* Each commit message must answer: **Why**, **What changed**, and **How it was proved**.


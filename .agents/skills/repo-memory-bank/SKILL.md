---
name: repo-memory-bank
description: Manage and enforce repository knowledge continuity (Memory Bank) across agentic workflows. Implements the 4th pillar of Spec-Driven Development (Continuity) by recording system patterns, learned lessons, and active context so agents never repeat past errors.
---

# Repository Memory Bank Skill (`repo-memory-bank`)

## 1. Overview and Intent

The **Memory Bank** guarantees **Continuity** across all autonomous agent invocations. As established in modern Agentic Software Factories (TLC Spec-Driven / Factory V2), an agent must never start from a blank slate without project memory, nor repeat mistakes previously diagnosed and fixed.

The Memory Bank lives in `.agents/memory/` and consists of three core artifacts:
1. `systemPatterns.md`: Consensual architectural patterns, tech stack rules, and code design guidelines.
2. `learnedLessons.md`: Post-mortem registry of errors, anti-patterns, framework traps, and verified fixes.
3. `activeContext.md`: Current sprint/task state, immediate next steps, and in-progress decisions.

---

## 2. When to Use

- **At the start of any Task or Planning Phase**: Read `systemPatterns.md` and `learnedLessons.md` to avoid forbidden anti-patterns and follow verified architectural choices.
- **During Bug Fixing / Triage**: Check `learnedLessons.md` to see if a similar symptom was resolved before.
- **During Pull Request Generation / DoD Closure**: Append new lessons learned, trap resolutions, or architectural pattern changes to keep the Memory Bank alive and updated.

---

## 3. Operational Protocols

### Phase 1: Ingestion (Read)
Before writing any code or formulating a Spec:
1. Load `.agents/memory/systemPatterns.md` to confirm naming conventions, layering, and libraries.
2. Load `.agents/memory/learnedLessons.md` to inspect known caveats (e.g. PowerShell quirks, test container setups, serialization edge cases).
3. Align implementation plan with `activeContext.md`.

### Phase 2: Updating & Recording (Write)
Whenever a non-trivial error is discovered and fixed (e.g., a tricky build error, a third-party API behavioral quirk, or a security audit rejection):
1. **Identify the Lesson**: State Context, Symptom, and the Mandatory Rule.
2. **Append to `learnedLessons.md`**: Format as a numbered section (`## Lesson N: ...`).
3. **Update `activeContext.md`**: Update current status, mark completed milestones, and define immediate next steps.

---

## 4. Quality Checklist

- [ ] Has the agent checked `systemPatterns.md` prior to architectural planning?
- [ ] Were relevant lessons from `learnedLessons.md` applied to prevent recurring defects?
- [ ] Were any newly diagnosed traps documented in `learnedLessons.md` before PR finalization?


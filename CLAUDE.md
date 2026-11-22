# Claude Code Entry Point

All rules for this repository live in `@AGENTS.md`. Follow them exactly. Do not duplicate rules here.

- Agents live in `.agents/agents/` and skills in `.agents/skills/` (exposed to Claude Code via `scripts/setup-claude-code.ps1`).
- Before planning, read `.agents/memory/learnedLessons.md` and `.agents/memory/activeContext.md`.
- The computational gate is `scripts/validate-all.ps1`; its exit code is the only accepted proof that checks pass.
- Repository artifacts are English (EN-US); chat with the user is Portuguese.

---
name: quality-gate
description: Checks to run before finishing any code change in this repo (build, format, warning count, tests). Use before you say you are done, or when the stop hook (Copilot `agentStop`, Claude/Codex `Stop`) reports a failed quality gate.
---

# Quality gate

Run these from the repo root, in order. All must pass before you finish.

1. `dotnet build` — no errors.
2. `dotnet format CRUDTasksWithAgent.slnx --verify-no-changes --include <changed .cs files>` — no formatting or style findings in files you touched (older files carry known debt listed in `docs/lint-baseline.md`). Fix with `dotnet format ... --include <files>` and review the diff.
3. `node scripts/lint-count.mjs --check` — the warning total matches `docs/lint-baseline.md`.
4. `dotnet test` — all tests pass.

## When a check fails

- Fix new warnings in the code you changed. Do not suppress them (`#pragma warning disable`, `[SuppressMessage]`, `.editorconfig` severity changes, `<NoWarn>`).
- Never raise the number in `docs/lint-baseline.md` to make the check pass.
- If you removed warnings, lower the baseline total and table to match and say so.
- If a failure is outside your change and you cannot fix it safely, stop and report it with the command output.

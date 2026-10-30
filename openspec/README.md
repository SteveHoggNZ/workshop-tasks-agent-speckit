# OpenSpec baseline

This folder holds the [OpenSpec](https://github.com/Fission-AI/OpenSpec) specs for the Task List app. OpenSpec is harness-agnostic: the specs are plain Markdown, and `openspec init` generates the slash commands/skills for whichever coding agent you use.

## What's here

```
openspec/specs/
  task-management/spec.md   task list UI + task service behaviour
  task-api/spec.md          REST contract under /api/tasks
  task-agent/spec.md        in-app agent tools + scripted model commands
```

These specs are the **source of truth for what the app is supposed to do**, written as requirements (`SHALL`) with `WHEN`/`THEN` scenarios. They were written to describe the current behaviour of `src/CRUDTasksWithAgent`. In the lab you use them to check the code against the specs, then change both together through OpenSpec changes (propose → apply → archive).

No harness-specific files are committed. Run `openspec init` for your own agent first.

## Set up your harness

From the repo root (Node.js required; nothing needs installing globally):

```bash
npx @fission-ai/openspec@latest init --tools <your-harness>
```

Common tool IDs: `claude`, `github-copilot`, `cursor`, `codex` (run `npx @fission-ai/openspec@latest init --help` for the full list; `--tools none` creates only the `openspec/` files). Then restart your IDE/agent so it picks up the new commands.

Verified with OpenSpec 1.15.0 (default `core` profile) on a copy of this repo. In every case `openspec/specs/**` was left byte-for-byte unchanged, and `validate` still passed afterwards.

| Harness | `--tools` | Files created (besides the common ones) | Start with |
| --- | --- | --- | --- |
| Claude Code | `claude` | `.claude/skills/openspec-*/SKILL.md` (6), `.claude/commands/opsx/*.md` (6) | `/opsx:propose "idea"` |
| GitHub Copilot | `github-copilot` | `.github/skills/openspec-*/SKILL.md` (6), `.github/prompts/opsx-*.prompt.md` (6) | `/opsx-propose "idea"` |
| Cursor | `cursor` | `.cursor/skills/openspec-*/SKILL.md` (6), `.cursor/commands/opsx-*.md` (6) | `/opsx-propose "idea"` |
| Codex | `codex` | `.agents/skills/openspec-*/SKILL.md` (6) and `.agents/skills/.openspec-target` (no commands; skills only) | `$openspec-propose "idea"` |

Common to all: `openspec/config.yaml` (`schema: spec-driven`, otherwise only comments) and `openspec/changes/archive/.gitkeep`.

The skills/commands cover propose, explore, apply, archive, sync and update. Extra workflows (new, continue, ff, bulk-archive, verify, onboard) can be added with `openspec config profile`.

GitHub Copilot extras: `init --tools github-copilot` may ask whether to set up the Copilot cloud coding agent. `--copilot-cloud` additionally creates `.github/workflows/copilot-setup-steps.yml` and `.github/agents/openspec.agent.md`; `--no-copilot-cloud` skips them. Use the flag to avoid a prompt in scripts.

## Validate

```bash
npx @fission-ai/openspec@latest validate --all --strict --no-interactive
```

Expected: `Totals: 3 passed, 0 failed (3 items)`. Other useful commands: `openspec list --specs`, `openspec show <capability> --type spec`.

Validation only checks that specs are well-formed. It does not check that the code matches them. Finding that out is part of the lab.

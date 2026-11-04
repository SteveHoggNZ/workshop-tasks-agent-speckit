---
name: lab-inspect-session
description: Inspect a Copilot agent session log to see whether repo instructions reached the model, which skills ran and which files were read. Use only when reviewing a workshop lab run or asked why an agent ignored AGENTS.md.
---

# Inspect a lab session

Copilot CLI and the VS Code Copilot target write logs to `~/.copilot/session-state/<id>/` (`workspace.yaml`, `events.jsonl`).

1. List recent sessions for this repo: `python3 .agents/skills/lab-inspect-session/scripts/inspect.py --list`
2. Inspect one (default: newest): `python3 .agents/skills/lab-inspect-session/scripts/inspect.py [<id>] [--marker "<text from AGENTS.md>"]`
3. Read the output:
   - `client`: `github/cli` = Copilot CLI, `vscode-agent-host` = VS Code Copilot target.
   - `instructions in system prompt`: false means the primitive never reached the model (cause a).
   - `skills`: a skill you did not expect (e.g. `speckit-*`) means it took over the prompt.
   - `read AGENTS.md`: whether the agent opened it itself.

Claude Code logs live in `~/.claude/projects/`; Codex in `~/.codex/sessions/`. The script only reads Copilot logs; for others, check the same three things by hand.

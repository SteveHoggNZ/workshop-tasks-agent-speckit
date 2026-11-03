# Agent compatibility: Copilot, Claude Code, Codex

The workshop works with GitHub Copilot (VS Code), Claude Code or OpenAI Codex. This page says where each tool looks for project files. It does not contain exercise answers.

Checked against Claude Code 2.1.284 and codex-cli 0.166.0. Anything marked **unverified** was not confirmed in official docs or a live check; test it yourself before relying on it.

## Matrix

Support: **yes**, **partial**, **no**, **unverified**. Sources are listed below the table.

| # | Primitive | Copilot (VS Code) | Claude Code | Codex |
|---|---|---|---|---|
| 1 | Repo instructions | `AGENTS.md`: yes [V] | `CLAUDE.md` with `@AGENTS.md`: yes. Reads `AGENTS.md` natively only when no `CLAUDE.md` exists [C1] | `AGENTS.md`: yes [X2] |
| 2 | Nested instructions | nested `AGENTS.md`: unverified | subfolder `CLAUDE.md`/`AGENTS.md`, loaded on demand: yes [C1] | nested `AGENTS.md`: unverified |
| 3 | Path-scoped rules | `.github/instructions/*.instructions.md` with `applyTo`: yes (Copilot extra) [V] | `.claude/rules/*.md` with `paths:`: yes [C1] | no glob rules; use a nested `AGENTS.md`: partial |
| 4 | Skills | `.github/skills/<name>/`: yes; `.agents/skills/<name>/`: yes [V] | `.claude/skills/<name>/`: yes; symlinked folder: yes; `.agents/skills`: no [C2] | `.agents/skills/<name>/`: yes; symlinks followed: yes [X1] |
| 5 | Prompt files | `.github/prompts/*.prompt.md`: yes (Copilot extra) [V] | a skill or `.claude/commands/*.md`: partial [C2] | a skill invoked as `$name`: partial [X1] |
| 6 | Custom agents | `.github/agents/*.agent.md` (`tools`, `handoffs`): yes [V] | `.claude/agents/*.md` (`name`, `description`, `tools`): yes [C3] | `.codex/agents/<name>.toml` (`name`, `description`, `developer_instructions`): yes; can't set sandbox/MCP per agent [X4] |
| 7 | Handoffs | `handoffs:` frontmatter, Local target only: yes (Copilot extra) [V] | no | no |
| 8 | Hooks | `.github/hooks/*.json`, `preToolUse`/`postToolUse`/`agentStop`; needs `chat.useHooks`: yes [V] | `.claude/settings.json` `hooks`, `PreToolUse`/`PostToolUse`/`Stop`: yes [C4] | `.codex/hooks.json` (or `[hooks]` in `.codex/config.toml`), `PreToolUse`/`PostToolUse`/`Stop`, Claude-style JSON; edits arrive as `apply_patch`; needs project trust + `/hooks` review: yes [X3] |
| 9 | Agent-scoped hooks | `hooks:` in agent frontmatter: yes [V] | `hooks:` in subagent frontmatter: yes [C3] | unverified |
| 10 | MCP (project) | `.vscode/mcp.json` (`servers`): yes [V] | `.mcp.json` (`mcpServers`): yes [C6] | `.codex/config.toml` `[mcp_servers.<id>]`, trusted projects only: yes [X5] |
| 11 | Plugin | `plugin.json` + `.github/plugin/marketplace.json`: partial; install from another workspace unverified [V] | `<plugin>/.claude-plugin/plugin.json` + `.claude-plugin/marketplace.json`: yes; symlinks inside plugins unverified [C5] | marketplace `.agents/plugins/marketplace.json`: partial; plugin manifest unverified [X2] |

Tool-name differences: a read-only agent uses `read`, `search` in Copilot and `Read, Grep, Glob` in Claude. MCP tools are `playwright/*` in Copilot and `mcp__playwright__*` in Claude. Agent frontmatter can't be shared between tools; share the body text only.

### Sources

- [C1] https://code.claude.com/docs/en/memory
- [C2] https://code.claude.com/docs/en/skills
- [C3] https://code.claude.com/docs/en/sub-agents
- [C4] https://code.claude.com/docs/en/hooks
- [C5] https://code.claude.com/docs/en/plugins
- [C6] https://code.claude.com/docs/en/mcp
- [X1] https://developers.openai.com/codex/skills (redirects to learn.chatgpt.com/docs/build-skills)
- [X2] https://learn.chatgpt.com/docs/config-file/config-reference (Codex config reference)
- [X3] https://learn.chatgpt.com/docs/hooks
- [X4] https://learn.chatgpt.com/docs/agent-configuration/subagents
- [X5] https://learn.chatgpt.com/docs/extend/mcp?surface=cli
- [V] VS Code Copilot customization docs: https://code.visualstudio.com/docs/copilot/customization/

## Portable recipe

The ready-made files in `examples/stepNN-<slug>/` follow this recipe; copy a folder to the repo root to install it. No tool reads `examples/` itself, though Claude Code and Copilot's nested-`AGENTS.md` option may pick up its `AGENTS.md`/`CLAUDE.md` files while you edit files inside `examples/`.

Keep **one canonical copy** of shared content. Each tool gets a symlink or a small adapter file (10 lines or fewer). Features only Copilot has (prompt files, `handoffs`, `applyTo`) are **Copilot extras**: use them if you are on Copilot, skip them otherwise.

| Primitive | Canonical copy | Adapters |
|---|---|---|
| Instructions (row 1) | `AGENTS.md` | `CLAUDE.md` containing the line `@AGENTS.md`. Prefer the import over a symlink because symlinks break on Windows clones. |
| Path-scoped rules (row 3) | `docs/rules/<topic>.md` | Copilot: `.github/instructions/<topic>.instructions.md` (`applyTo` + text or link). Claude: `.claude/rules/<topic>.md` (`paths:`), or a symlink to the canonical file. Whether `@import` works inside rules files is unverified. Codex: nested `AGENTS.md` in the target folder. |
| Skills (row 4) | `.agents/skills/<name>/SKILL.md` | Claude: symlink `.claude/skills/<name>`. Copilot: reads `.agents/skills` directly. Keep frontmatter to `name` and `description`; `name` must match the folder name. |
| Custom agents (row 6) | body text (no frontmatter) | One file per tool with its own frontmatter: `.github/agents/<name>.agent.md`, `.claude/agents/<name>.md`, `.codex/agents/<name>.toml` (`developer_instructions`). |
| Hooks (row 8) | `scripts/hooks/<name>.sh --tool copilot\|claude\|codex` | Per-tool config: `.github/hooks/<file>.json`, `.claude/settings.json` `hooks`, `.codex/hooks.json`. The script reads both `tool_name`/`toolName` and `tool_input`/`toolArgs`, then writes the output format of the tool given in `--tool`. |
| MCP (row 10) | one server key, e.g. `playwright`, with the same pinned version | `.vscode/mcp.json`, `.mcp.json`, `.codex/config.toml`. |
| Plugin (row 11) | Claude layout `plugins/<plugin>/.claude-plugin/plugin.json` with `skills/`, `agents/`, `hooks/` inside the plugin folder | Copilot `plugin.json` alongside. Every path must stay inside the plugin folder. Symlink support inside plugins is unverified. Codex plugin: optional. |

## Create a symlink

Use relative targets so the link works in every clone. Run from the repo root:

```bash
mkdir -p .claude/skills
ln -s ../../.agents/skills/<name> .claude/skills/<name>
ls -l .claude/skills/<name>          # shows the target
test -e .claude/skills/<name>/SKILL.md && echo ok
```

The target is relative to the link's folder, not to your shell. Git stores symlinks as links. On Windows, enable `git config core.symlinks true` (and Developer Mode) before cloning, or use adapter files instead. To find broken links: `find . -type l ! -exec test -e {} \; -print`.

## Per-tool setup

### GitHub Copilot (VS Code)

- Picks up `AGENTS.md`, `.github/instructions`, `.github/prompts`, `.github/skills`, `.agents/skills`, `.github/agents`, `.github/hooks` and `.vscode/mcp.json`. Whether it also reads nested `AGENTS.md`, `CLAUDE.md` or `.claude/*` is unverified.
- Use the **Local** session target. Handoffs only work there.
- Hooks: set `"chat.useHooks": true`. The Local target accepts Copilot event names (`preToolUse`) but sends Local payloads (`tool_name`, `tool_input`). Results show in the GitHub Copilot Chat Hooks output channel.
- MCP: start the server from the MCP view. Agent `tools` refer to it as `<server-key>/*`.
- Check: the References list in a chat response shows which instruction files loaded.

### Claude Code

- Picks up `CLAUDE.md` (and its `@` imports), `.claude/rules`, `.claude/skills`, `.claude/agents`, `.claude/settings.json` and `.mcp.json`. It does not read `.agents/`.
- Hooks: `.claude/settings.json` under `"hooks"`, events `PreToolUse`, `PostToolUse`, `Stop`. Input is snake_case (`tool_name`, `tool_input.file_path`). To block: exit code 2 (stderr goes to Claude) or JSON `hookSpecificOutput.permissionDecision: "deny"`. Use `${CLAUDE_PROJECT_DIR}` in commands. Hooks need workspace trust.
- MCP: approve the project servers from `.mcp.json` when prompted; check with `/mcp`.
- Check: `/memory` lists loaded instruction files, `/agents` lists agents, `/` lists skills.

### OpenAI Codex

- Picks up `AGENTS.md` and `.agents/skills`. Project `.codex/config.toml` (MCP, hooks, agents) loads **only when the project is trusted**.
- Hooks: on by default (`[features] hooks = false` turns them off). Put them in `.codex/hooks.json`, same shape as Claude's `hooks` block. Trust the project, then review and trust each hook with `/hooks`. Stdin has `tool_name` (`apply_patch`, `Bash`, `mcp__x__y`) and `tool_input`; file edits arrive as `apply_patch` with the patch in `tool_input.command`. Block with exit 2 or `hookSpecificOutput.permissionDecision: "deny"`. Commands run in the session folder, so use a git-root path.
- MCP: `[mcp_servers.playwright]` with `command = "npx"` and `args = ["-y", "@playwright/mcp@0.0.84"]`. Check with `codex mcp list` or `/mcp`.
- Custom agents are TOML files in `.codex/agents/<name>.toml` with `name`, `description`, `developer_instructions`, not markdown. They inherit sandbox and MCP settings from the session. No handoffs.
- Check: `/skills` lists skills; invoke one with `$name`.

## Unverified

- Copilot: reads `CLAUDE.md`, `.claude/*`, `.mcp.json` or nested `AGENTS.md`; plugin install from another workspace.
- Claude Code: symlinks inside plugins; `@import` inside `.claude/rules` files.
- Codex: nested `AGENTS.md`/`AGENTS.override.md` rules; plugin manifest; handling of unknown `SKILL.md` frontmatter.

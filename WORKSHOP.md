# Workshop: Coding-agent customization and advanced workflows

Two sessions. In each step you see a problem, build an agent primitive, and use it to fix the problem. Use GitHub Copilot, Claude Code or OpenAI Codex.

Where each tool expects each file: [agent compatibility matrix](docs/agent-compatibility.md#matrix). Paths below use the Copilot name first; the matrix row gives the Claude and Codex paths. **Copilot extra** marks features other tools skip.

For inspiration, browse [github/awesome-copilot](https://github.com/github/awesome-copilot). Write your own files; do not copy them in wholesale.

## Setup

1. Install the .NET 10 SDK, Node.js 22+, Git and your agent: VS Code Insiders with GitHub Copilot, Claude Code, or Codex CLI.
2. Install the diagnostics tools:
   `for t in dotnet-counters dotnet-trace dotnet-gcdump dotnet-stack; do dotnet tool install -g $t; done`
3. Create your branch: `git switch workshop/baseline && git switch -c lab/<your-name>`.
4. Run the app: `dotnet run --project src/CRUDTasksWithAgent`. Open http://localhost:5280.
5. Run the tests once: `dotnet test`.
6. Commit after each step. Use a short message such as `lab: step 1 AGENTS.md`.
7. Note: task data lives in memory. Restart the app to reset it.
8. For Step 7 offline: `npx -y @playwright/mcp@0.0.84 --help && npx playwright install chromium`. Pin the same version everywhere.
9. Enable hooks (Steps 10-11): Copilot `"chat.useHooks": true`; Claude trust the workspace; Codex trust the project, then trust each hook with `/hooks`. See [per-tool setup](docs/agent-compatibility.md#per-tool-setup).

### Session target (Copilot)

Use the **Local** session target in Copilot Chat for this workshop. Agent handoffs and prompt files only work there. Claude Code and Codex: run from the repo root in a new session per step.

### How each step works

**Before/After**: what you see now and what you should see once your primitive works. **Symptom**: what to do and what to look for. **Create**: the file to add and what it should cover; you write the content. **Prove it**: run the same prompt or measurement before and after, in a new chat, and compare. "Agent mode" means your tool's normal editing agent.

## Session 1: Instructions and agents

### Step 1: Project instructions (`AGENTS.md`)
**Before:** the generated `/completed` page injects `TasksDbContext` and prints `DateTime.Now`, like `/recent`.\
**After:** the generated page uses `TaskService` and `TimeProvider`, and so does `/recent` once the agent fixes it.

**Symptom**

1. Open http://localhost:5280/recent. Note what it shows.
2. In Agent mode, ask: `Add a /completed page that lists completed tasks and shows when the page was loaded.`
3. Review the code it writes. Compare it with the rest of the app. Would you approve it?
4. Discard the change: `git restore . && git clean -fd src tests`.

**Create**

`AGENTS.md` at the repo root, under 60 lines (see matrix row 1; Claude also needs `CLAUDE.md` containing `@AGENTS.md`). Cover the stack and folder layout, how components get data, how code gets the current time, build and test commands, and folders the agent must not touch without asking.

**Prove it**

Run the same prompt in a new chat. The new page should follow your rules, not the patterns in `/recent`. Then ask the agent to fix `/recent` to match.

### Step 2: File-scoped instructions (`*.instructions.md`)
**Before:** the review misses what you saw on `/board` and `/dashboard`; no Razor rules file is loaded (Copilot: References list).\
**After:** your Razor rules file is loaded (Copilot: References lists `blazor.instructions.md`), and the review and new component follow each of your rules.

**Symptom**

1. Open http://localhost:5280/board.
2. Tick the first task. Watch every checkbox after the list reorders.
3. Untick it again. Watch again.
4. Open http://localhost:5280/dashboard. Move between Dashboard and other pages ten times. Nothing visible happens; Step 9 measures it.
5. Ask the agent: `Review Board.razor and Dashboard.razor for Blazor problems.` Note what it misses.

**Create**

A rules file that loads only for Razor files (see matrix row 3): Copilot `.github/instructions/blazor.instructions.md` with `applyTo` (Copilot extra); Claude `.claude/rules/blazor.md` with `paths:`; Codex a nested `AGENTS.md`. List the Blazor rules you want every component to follow, one for each problem you saw, plus data access.

Optionally add a rules file for files under `tests/`.

**Prove it**

1. Open `Board.razor` and repeat the review prompt in a new chat. Your rules file should load. With only `Program.cs` open, it should not.
2. Ask the agent to add a new component with a list and an event subscription. Check it follows your rules.

Do not fix the bugs yet. Steps 7, 8 and 9 cover them.

### Step 3: Skill vs prompt file (`add-agent-tool`)
**Before:** `clear completed` in `/agentfx` is not understood, or no new test exists; note how many of the files you found were changed.\
**After:** `clear completed` works, `dotnet test` shows one or more new passing tests, and every file in your skill was changed.

**Symptom**

1. Open http://localhost:5280/agentfx. Type `help`. Note the commands the scripted model understands.
2. Ask the agent: `Add an app-agent tool that deletes all completed tasks. Users should be able to type "clear completed".`
3. Check: Does `clear completed` work in `/agentfx`? Is there a test? Run `dotnet test`. Count the files changed.
4. Discard the change: `git restore . && git clean -fd src tests`.

**Create**

When: a prompt file is a command you start and fill in; a skill is picked by the agent from its `description`.

1. Copilot extra: `.github/prompts/new-agent-tool.prompt.md`. Ask for the tool name and description with `${input:...}` variables (see matrix row 5). A different name from the skill keeps the two `/` commands apart.
2. `.agents/skills/add-agent-tool/SKILL.md` (see matrix row 4; Claude: symlink `.claude/skills/add-agent-tool`). Write the full procedure: every file to change, in order, with exact paths, and how to check the result.

Find the files yourself. Start from `src/CRUDTasksWithAgent/Tools/` and `tests/`.

**Prove it**

1. Copilot: run the prompt file with `/new-agent-tool`. Check the same things as in the Symptom.
2. Discard. Ask the plain prompt from the Symptom again in a new chat. The chat should show the `add-agent-tool` skill loaded before any edit.
3. Write down one difference between a prompt file and a skill.

### Step 4: Read-only reviewer agent (`docs-reviewer`)
**Before:** your own list of drift items between `docs/architecture.md` and the code (write down the count).\
**After:** the agent's list covers at least your items, it cannot edit the doc, and a re-review after the fix reports no drift.

**Symptom**

1. Read `docs/architecture.md`.
2. Compare it with the folders and pages in `src/CRUDTasksWithAgent`.
3. Note anything missing or wrong.

**Create**

`.github/agents/docs-reviewer.agent.md` (see matrix row 6; Claude `.claude/agents/docs-reviewer.md`). Give it read and search tools only. Tell it to compare the docs with the code and report drift as a list. It must never edit files.

**Prove it**

1. Select the `docs-reviewer` agent. Ask: `Review docs/architecture.md.`
2. Compare its list with yours.
3. Ask it to fix the doc. It should refuse or have no edit tool.
4. Switch to the default agent and fix the doc using the report.

### Step 5: Package it (plugin)
**Before:** another workspace's agent picker shows none of your agents or skills.\
**After:** after installing `tasks-lab`, your agents appear in the picker and your skills load there.

**Symptom**

Your primitives are spread across `AGENTS.md` and several tool folders. A teammate on another repo cannot reuse them easily.

**Create** (see matrix row 11)

1. A `tasks-lab` plugin manifest that lists your agents, skills and (later) hooks: Copilot `plugins/tasks-lab/plugin.json`; Claude `plugins/tasks-lab/.claude-plugin/plugin.json`. Every path must stay inside `plugins/tasks-lab/`.
2. A marketplace file that lists the plugin: Copilot `.github/plugin/marketplace.json`; Claude `.claude-plugin/marketplace.json`.

For Copilot, match the format used in [github/awesome-copilot](https://github.com/github/awesome-copilot) (`plugins/` and `.github/plugin/`).

**Prove it**

1. Add your repo as a plugin marketplace in your tool.
2. Install `tasks-lab` from it in another workspace.
3. Check the agents and skills appear there.

Come back to this step after Session 2 to add the new skill and hooks.

---

## Session 2: Advanced workflows

### Step 6: TDD agents with handoffs (`tdd-red`, `tdd-green`, `tdd-refactor`)
**Before:** Dashboard shows `NaN%` with no tasks; `dotnet test` has 0 tests for `TaskStatsCalculator`; after `tdd-red`, 1 failing.\
**After:** 0 failing tests, at least 2 new passing ones, `0%` with no tasks, and the right "Week starting" date every day.

**Symptom**

1. Open http://localhost:5280/. Delete every task.
2. Open http://localhost:5280/dashboard. Read the completion rate.
3. A user also reports that "Week starting" shows the wrong date on some days of the week.
4. Look in `tests/`. Is `TaskStatsCalculator` tested?

**Create**

Three agents (see matrix row 6) in `.github/agents/` (Claude `.claude/agents/`):

- `tdd-red.agent.md`: writes one failing test. It may only edit files under `tests/`. It hands off to `tdd-green`.
- `tdd-green.agent.md`: writes the smallest code change that makes the test pass. It hands off to `tdd-refactor`.
- `tdd-refactor.agent.md`: tidies code and tests without changing behaviour. All tests must stay green.

Copilot extra: use the `handoffs` frontmatter so a button appears at the end of each turn (matrix row 7). Other tools: switch agent yourself (Claude `@agent-tdd-green`).

**Prove it**

1. Select `tdd-red`. Describe one bug from the Symptom.
2. Confirm the new test fails for the right reason.
3. Hand off (or switch) to `tdd-green`. Confirm the test passes.
4. Hand off (or switch) to `tdd-refactor`.
5. Repeat for the second bug.
6. Check the dashboard again.

### Step 7: Debugger agent with Playwright MCP
**Before:** after ticking the first task, a different row's checkbox is ticked; the struck-through rows and the reload disagree.\
**After:** checkboxes match the struck-through rows before and after reload, and the agent showed the bug in the browser first.

**Symptom**

1. Open http://localhost:5280/board.
2. Tick the first task. Note which checkboxes look ticked and which tasks are struck through.
3. Reload the page. Compare.

**Create**

1. A project MCP config that starts the Playwright MCP server (`npx -y @playwright/mcp@0.0.84`), server key `playwright` (see matrix row 10): `.vscode/mcp.json`, `.mcp.json` or `.codex/config.toml`. Start or approve it in your tool.
2. `.github/agents/debugger.agent.md` (Claude `.claude/agents/debugger.md`). Give it a fixed loop: reproduce, hypothesise, instrument, fix, add a regression check. Give it the Playwright tools (Copilot `playwright/*`, Claude `mcp__playwright__*`; must match the server key) so it can drive the browser itself.

**Prove it**

1. Keep the app running.
2. Select `debugger`. Ask: `On /board, ticking the first task leaves the wrong checkboxes ticked. Reproduce it in the browser and fix it.`
3. Watch it reproduce the bug before it changes code.
4. Repeat the Symptom steps yourself.

### Step 8: Diagnostics skill: slow page
**Before:** visible lag on each keystroke and `[` breaks the page; record CPU %, the settle time and the top frame from the trace.\
**After:** no visible lag (settles in about 1 s), `[` is a normal search, CPU % is lower and the old top frame is gone from the top 15.

**Symptom**

1. Open http://localhost:5280/bulk. Click **Generate 5,000 sample tasks**.
2. Type `sample task 12` into Search, one key at a time. Note how long the count takes to settle.
3. Type `[`, then a few more keys. The count stops updating; there is no error message.

**Create**

A `dotnet-diagnostics` skill (see matrix row 4) with a `scripts/` folder. Add one small script per job: find the app's process id (`dotnet-counters ps`); collect counters as JSON; collect a CPU trace in speedscope format; summarise a speedscope file into the top 15 frames; collect and report a GC heap dump (used in Step 9).

Starter commands (bound each with `--duration dd:hh:mm:ss`):

- `dotnet-counters collect -p <pid> --counters 'System.Runtime[cpu-usage,gc-heap-size]' --format json -o counters.json`. Name the counters: plain `System.Runtime` on .NET 9+ has no `cpu-usage` or `gc-heap-size`.
- `dotnet-trace collect -p <pid> --format speedscope -o trace.nettrace`. When summarising, add `UNMANAGED_CODE_TIME` leaf time to the managed caller, or the ranking is meaningless.

Make scripts executable. Use `set -euo pipefail`. Bound every collection by time. The skill should tell the agent which script to run for which symptom.

**Prove it**

1. Before any fix: ask the agent to use the skill to measure `/bulk` while you type in Search. Record CPU usage, settle time and the top frames.
2. Ask the agent to fix the page using the measurements.
3. Measure again with the same steps. Compare the numbers.

### Step 9: Diagnostics skill: memory growth
**Before:** GC heap size grows by tens of MB over 20 Dashboard visits and does not drop; record the MB.\
**After:** the same 20 visits leave the heap roughly flat (a few MB), and the second heap dump shows no growth.

**Symptom**

1. Restart the app.
2. Start `dotnet-counters monitor -n CRUDTasksWithAgent`. Watch the GC heap size.
3. Open http://localhost:5280/dashboard. Move to another page and back 20 times.
4. Tick and untick a task on `/board` a few times.
5. Wait a minute. Does the heap size drop back?

**Create**

Extend `dotnet-diagnostics` if needed (see matrix row 4). The skill should explain how to take two heap dumps and compare them.

**Prove it**

1. Ask the agent to use the skill: take a heap dump, repeat steps 3 and 4, take another, and compare.
2. Ask it to fix the cause it finds.
3. Repeat the Symptom. The heap should level off.

### Step 10: Hooks (guard rails)
**Before:** `git status` shows `tdd-red` edited `src/` and the agent edited `infra/main.bicep`.\
**After:** both edits are blocked with a reason, `git status` shows no `src/` or `infra/` changes, and a syntax error is reported.

**Symptom**

1. Select `tdd-red`. Ask: `Write a failing test for an empty task title, then make it pass.` Note whether it edits files under `src/`.
2. In the default agent, ask: `Change the App Service SKU in infra/main.bicep to P1v3.` Note whether it edits the file.
3. Discard both changes.

**Create** (see matrix rows 8 and 9; enable hooks first, Setup 9)

1. A hook config and scripts: Copilot `.github/hooks/lab-guards.json` + `.github/hooks/scripts/`; Claude `.claude/settings.json` `hooks`; Codex `.codex/hooks.json` (see matrix row 8).
2. A pre-tool hook (Copilot `preToolUse`, Claude/Codex `PreToolUse`) that blocks edits under `infra/`.
3. A pre-tool hook that blocks edits outside `tests/` while a `.tdd-red` marker file exists, including multi-file edits. Update `tdd-red` to create the marker and `tdd-green` to remove it. Add `.tdd-red` to `.gitignore`.
4. A post-tool hook (`postToolUse` / `PostToolUse`) that runs a quiet build after a `.cs` or `.razor` edit and reports errors to the agent without blocking.

Check your tool's hooks docs for the input and output format (Copilot Local target: the Local hooks reference; input may use `tool_name`/`tool_input` or `toolName`/`toolArgs`). Test each script by piping sample JSON into it.

**Prove it**

1. Repeat both Symptom prompts in new chats.
2. The edits should be blocked with a clear reason.
3. Make a deliberate syntax error through the agent. The agent's next message should mention the build error.

### Step 11: Quality gate
**Before:** the Problems panel and `node scripts/lint-count.mjs` show 24; the agent can add a page that raises it and still finish.\
**After:** the count is 19 or lower, `--check` passes, format verify passes for the files you changed, and the agent will not finish with new warnings.

**Symptom**

1. Run `node scripts/lint-count.mjs`. Compare the total with `docs/lint-baseline.md`. Run `node scripts/lint-count.mjs --check`.
2. Run `dotnet format CRUDTasksWithAgent.slnx --verify-no-changes --include <changed files>`. Note the exit code. The whole solution fails this check even when finished, so always pass `--include`. Do not run it without `--verify-no-changes`.
3. Ask the agent: `Add an /archive page that lists completed tasks.` Run `node scripts/lint-count.mjs` again. Did the count rise? Did the build fail?
4. Discard the change: `git restore . && git clean -fd src tests`.

**Create**

1. A `quality-gate` skill (see matrix row 4): the checks to run before finishing (build, format verify on changed files, lint count check, tests), and the rule to fix new warnings rather than suppress them or raise the baseline.
2. A stop hook (Copilot `agentStop`, Claude/Codex `Stop`; see matrix row 8) that runs the lint count check and format verify on changed files, and tells the agent to keep working if either fails. Check your tool's docs for the exact event name and block output.
3. Promote nullable warnings to errors in `Directory.Build.props`. Fix what breaks, then update `docs/lint-baseline.md` to the new total.

**Prove it**

1. Repeat the Symptom prompt in a new chat. The agent should not finish while the count is above the baseline: look for a second agent turn after it tries to stop.
2. `node scripts/lint-count.mjs --check` passes and `dotnet build` shows no nullable warnings.

Then return to Step 5 and add the diagnostics skill, quality-gate skill and hooks to your plugin. Finish with `dotnet build` and `dotnet test`.

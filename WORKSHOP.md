# Workshop: Coding-agent customization and advanced workflows

Two sessions. In each step you see a problem, install a ready-made agent primitive, read it, and run the same prompt again to see what changed. Use GitHub Copilot, Claude Code or OpenAI Codex.

Where each tool expects each file: [agent compatibility matrix](docs/agent-compatibility.md#matrix). Every example folder installs the Copilot, Claude Code and Codex files at once; the matrix row tells you which ones your tool reads. **Copilot extra** marks features other tools skip. More examples: [github/awesome-copilot](https://github.com/github/awesome-copilot).

## Setup

1. Install the .NET 10 SDK, Node.js 22+, Git and your agent: VS Code Insiders with GitHub Copilot, Claude Code, or Codex CLI.
2. Install the diagnostics tools:
   `for t in dotnet-counters dotnet-trace dotnet-gcdump dotnet-stack; do dotnet tool install -g $t; done`
3. Create your branch: `git switch main && git switch -c lab/<your-name>`.
4. Run the app: `dotnet run --project src/CRUDTasksWithAgent`. Open http://localhost:5280.
5. Run the tests once: `dotnet test`.
6. Note: task data lives in memory. Restart the app to reset it.
7. For Step 7 offline: `npx -y @playwright/mcp@0.0.84 --help && npx playwright install chromium`. Pin the same version everywhere.
8. Enable hooks (Steps 10-11): Copilot `"chat.useHooks": true`; Claude trust the workspace; Codex trust the project, then trust each hook with `/hooks`. See [per-tool setup](docs/agent-compatibility.md#per-tool-setup).

### Session target (Copilot)

Use the **Local** session target in Copilot Chat for this workshop. Agent handoffs and prompt files only work there. Claude Code and Codex: run from the repo root in a new session per step.

### How each step works

1. **Before**: run the prompt or measurement and write down the score.
2. **Reset**: throw away the Before changes so they are not committed: `git restore . && git clean -fd src tests`.
3. **Copy**: install the example from `examples/stepNN-<slug>/` and commit it on its own:
   `cp -R examples/stepNN-<slug>/. ./ && git add -A && git commit -m "lab: stepNN copy example"`
4. **Read**: open each installed file. Look for the points listed.
5. **After**: start a **new** chat or session (the old one has not loaded the new files), run the same prompt, and compare.
6. **Try** (optional): make one small edit to the example and run again.
7. **Commit**: keep the After result if it is good (`git add -A && git commit -m "lab: stepNN after"`), or discard it with the reset command. Either way, end each step with a clean `git status`.

The reset command only removes uncommitted changes, never the examples you committed. Your commit history then shows each step as "copy example" followed by "after", which is easy to compare with `git diff`. "Agent mode" means your tool's normal editing agent. Copying the folders in order gives the full solution.

## Session 1: Instructions and agents

### Step 1: Project instructions (`AGENTS.md`)
**Before:** the generated `/completed` page scores 0–2 on the checklist below (typically copies `/recent`: `TasksDbContext`, `DateTime.Now`, no test).\
**After:** the same prompt scores 4–5, and `/recent` matches once the agent fixes it.

**Before**

1. Open http://localhost:5280/recent. Note what it shows.
2. In Agent mode, ask: `Add a /completed page, like the Recent page, that lists completed tasks and shows when the page was loaded.`
3. Score the change, one point each:
   - Data comes through the app's service, not straight from the database context.
   - The current time comes from an injectable clock, not `DateTime.Now`/`DateTimeOffset.Now`.
   - A test was added.
   - The agent ran `node scripts/lint-count.mjs` and reported the result.
   - `docs/architecture.md` lists the new page.
4. Models vary. If it scores 3 or more, run it once more in a new chat and keep the lower score.
5. Reset.

**Copy** `cp -R examples/step01-agents-md/. ./ && git add -A && git commit -m "lab: step01 copy example"` → `AGENTS.md`, `CLAUDE.md` (matrix row 1).

**Read**

- `AGENTS.md` loads into every chat, so it holds only what applies to every task: stack, layout, rules, commands.
- Each rule answers one checklist line: `TaskService` only, `TimeProvider`, the "Definition of done" list.
- It is short (under 60 lines). Long files dilute the rules that matter.
- `CLAUDE.md` is a one-line adapter (`@AGENTS.md`) so Claude Code reads the same text.

**After** Run the same prompt in a new chat and score it again. The new page should follow the rules, not the patterns in `/recent`. Then ask the agent to fix `/recent` to match.

**Try** Delete the `TimeProvider` rule, reset, and run again. Does `DateTime.Now` come back?

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step01 after"` (or reset to discard the After run). `git status` should be clean before Step 2.

### Step 2: File-scoped instructions (`*.instructions.md`)
**Before:** the review misses what you saw on `/board` and `/dashboard`; no Razor rules file is loaded (Copilot: References list).\
**After:** your Razor rules file is loaded (Copilot: References lists `blazor.instructions.md`), and the review and new component follow each of your rules.

**Before**

1. Open http://localhost:5280/board. Tick the first task. Watch every checkbox after the list reorders. Untick it and watch again.
2. Open http://localhost:5280/dashboard. Move between Dashboard and other pages ten times. Nothing visible happens; Step 9 measures it.
3. Ask the agent: `Review Board.razor and Dashboard.razor for Blazor problems.` Note what it misses.

**Copy** `cp -R examples/step02-scoped-rules/. ./ && git add -A && git commit -m "lab: step02 copy example"` → `docs/rules/{blazor,tests}.md`, `.github/instructions/{blazor,tests}.instructions.md`, `.claude/rules/` (symlinks), `src/CRUDTasksWithAgent/Components/AGENTS.md`, `tests/AGENTS.md` (matrix row 3).

**Read**

- `docs/rules/blazor.md` is the one canonical copy: one rule per problem you saw (`@key`, unsubscribe in `Dispose`, `InvokeAsync`) plus data access.
- Scoped rules load only when matching files are in play: `applyTo` (Copilot extra), `paths:` (Claude), a nested `AGENTS.md` (Codex).
- The per-tool files are adapters: a link, a symlink or one sentence. Edit the rule once.
- `tests.md` shows the same pattern for `tests/**`.

**After**

1. Open `Board.razor` and repeat the review prompt in a new chat. The rules file should load. With only `Program.cs` open, it should not.
2. Ask the agent to add a new component with a list and an event subscription. Check it follows each rule.

Do not fix the bugs yet. Steps 7, 8 and 9 cover them.

**Try** Narrow `applyTo`/`paths:` to `**/Board.razor` and check whether the rules still load for `Dashboard.razor`.

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step02 after"` (or reset to discard the After run). `git status` should be clean before Step 3.

### Step 3: Skill vs prompt file (`add-agent-tool`)
**Before:** `clear completed` in `/agentfx` is not understood, or no new test exists; note how many of the files you found were changed.\
**After:** `clear completed` works, `dotnet test` shows one or more new passing tests, and every file in the skill was changed.

**Before**

1. Open http://localhost:5280/agentfx. Type `help`. Note the commands the scripted model understands.
2. Ask the agent: `Add an app-agent tool that deletes all completed tasks. Users should be able to type "clear completed".`
3. Check: Does `clear completed` work in `/agentfx`? Is there a test? Run `dotnet test`. Count the files changed.
4. Reset.

**Copy** `cp -R examples/step03-skill-and-prompt/. ./ && git add -A && git commit -m "lab: step03 copy example"` → `.agents/skills/add-agent-tool/SKILL.md`, `.claude/skills/add-agent-tool` (symlink), `.github/prompts/new-agent-tool.prompt.md` (matrix rows 4-5).

**Read**

- A skill is picked by the agent from its `description`; write the description as "use when ...".
- The body lists every file to change, in order, with exact paths, plus how to check the result.
- The prompt file (Copilot extra) is a command you start and fill in with `${input:...}`. It has a different name so the two `/` commands stay apart.
- The Claude symlink points at the one canonical skill folder.

**After**

1. Copilot: run `/new-agent-tool`. Check the same things as in Before. Reset.
2. Ask the plain prompt from Before in a new chat. The chat should show the `add-agent-tool` skill loaded before any edit.
3. Write down one difference between a prompt file and a skill.

**Try** Make the skill `description` vague ("Helps with tools") and see whether it still loads for the plain prompt.

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step03 after"` (or reset to discard the After run). `git status` should be clean before Step 4.

### Step 4: Read-only reviewer agent (`docs-reviewer`)
**Before:** your own list of drift items between `docs/architecture.md` and the code (write down the count).\
**After:** the agent's list covers at least your items, it cannot edit the doc, and a re-review after the fix reports no drift.

**Before** Read `docs/architecture.md`, compare it with the folders and pages in `src/CRUDTasksWithAgent`, and list anything missing or wrong.

**Copy** `cp -R examples/step04-docs-reviewer/. ./ && git add -A && git commit -m "lab: step04 copy example"` → `.agents/agents/docs-reviewer.md`, `.github/agents/docs-reviewer.agent.md`, `.claude/agents/docs-reviewer.md`, `.codex/agents/docs-reviewer.toml` (matrix row 6).

**Read**

- Use a custom agent when you want a fixed role with fewer tools than the default agent.
- The body (`.agents/agents/`) is shared; each tool file only adds frontmatter.
- Read-only comes from the tool list: `read`, `search` (Copilot), `Read, Grep, Glob` (Claude). Codex can only ask in text.
- The report format (Wrong / Missing / total) makes runs comparable.

**After**

1. Select the `docs-reviewer` agent. Ask: `Review docs/architecture.md.` Compare its list with yours.
2. Ask it to fix the doc. It should refuse or have no edit tool.
3. Switch to the default agent and fix the doc using the report. Re-run the review.

**Try** Add `edit` to the Copilot `tools` list (or `Edit` for Claude) and ask it to fix the doc again.

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step04 after"` (or reset to discard the After run). `git status` should be clean before Step 5.

### Step 5: Package it (plugin)
**Before:** another workspace's agent picker shows none of your agents or skills.\
**After:** after installing `tasks-lab`, your agents appear in the picker and your skills load there.

**Before** Open another workspace. Your agents and skills are not there; the primitives are spread across `AGENTS.md` and several tool folders.

**Copy** `cp -R examples/step05-plugin/. ./ && git add -A && git commit -m "lab: step05 copy example"` → `plugins/tasks-lab/` (`plugin.json`, `.claude-plugin/plugin.json`, symlinks to agents and skills), `.github/plugin/marketplace.json`, `.claude-plugin/marketplace.json` (matrix row 11).

**Read**

- A plugin bundles agents, skills and hooks so another repo can install them in one go.
- Every manifest path stays inside `plugins/tasks-lab/`; symlinks point back to the canonical files.
- The marketplace file is the catalogue your tool adds; the plugin is what it installs.
- Copilot follows the [github/awesome-copilot](https://github.com/github/awesome-copilot) layout.

**After** Add your repo as a plugin marketplace in your tool, install `tasks-lab` in another workspace, and check the agents and skills appear.

Step 11's example updates the plugin with the Session 2 skills, agents and hooks.

---

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step05 after"` (or reset to discard the After run). `git status` should be clean before Step 6.

## Session 2: Advanced workflows

### Step 6: TDD agents with handoffs (`tdd-red`, `tdd-green`, `tdd-refactor`)
**Before:** Dashboard shows `NaN%` with no tasks; `dotnet test` has 0 tests for `TaskStatsCalculator`; after `tdd-red`, 1 failing.\
**After:** 0 failing tests, at least 2 new passing ones, `0%` with no tasks, and the right "Week starting" date every day.

**Before**

1. Open http://localhost:5280/. Delete every task. Open http://localhost:5280/dashboard and read the completion rate.
2. A user also reports that "Week starting" shows the wrong date on some days of the week.
3. Look in `tests/`. Is `TaskStatsCalculator` tested?

**Copy** `cp -R examples/step06-tdd-agents/. ./ && git add -A && git commit -m "lab: step06 copy example"` → `tdd-red`, `tdd-green`, `tdd-refactor` in `.agents/agents/`, `.github/agents/`, `.claude/agents/`, `.codex/agents/` (matrix rows 6-7).

**Read**

- Three narrow agents keep each phase honest: red writes one failing test under `tests/` only; green makes the smallest change; refactor keeps tests green.
- `handoffs` (Copilot extra) put a button at the end of each turn. Other tools: switch agent yourself (Claude `@agent-tdd-green`).
- "Fails for the right reason" stops a compile error passing as red.
- Red only *asks* not to touch `src/`; Step 10 enforces it.

**After**

1. Select `tdd-red`. Describe one bug from Before. Confirm the new test fails for the right reason.
2. Hand off (or switch) to `tdd-green`, then `tdd-refactor`. Confirm all tests pass.
3. Repeat for the second bug. Check the dashboard again.

**Try** Remove "Stop after one failing test" from `tdd-red` and see how many tests it writes.

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step06 after"` (or reset to discard the After run). `git status` should be clean before Step 7.

### Step 7: Debugger agent with Playwright MCP
**Before:** after ticking the first task, a different row's checkbox is ticked; the struck-through rows and the reload disagree.\
**After:** checkboxes match the struck-through rows before and after reload, and the agent showed the bug in the browser first.

**Before**

1. Open http://localhost:5280/board. Tick the first task. Note which checkboxes look ticked and which tasks are struck through.
2. Reload the page. Compare.
3. Ask the default agent the prompt from After. Note whether it can see the browser.

**Copy** `cp -R examples/step07-debugger-mcp/. ./ && git add -A && git commit -m "lab: step07 copy example"` → `.vscode/mcp.json`, `.mcp.json`, `.codex/config.toml`, `debugger` in `.agents/agents/`, `.github/agents/`, `.claude/agents/`, `.codex/agents/` (matrix rows 6, 10). Start or approve the `playwright` server in your tool.

**Read**

- MCP gives the agent new tools; here, a real browser. One server key (`playwright`) and one pinned version in all three configs.
- The agent's tool names must match the key: `playwright/*` (Copilot), `mcp__playwright__*` (Claude).
- The fixed loop (reproduce, hypothesise, instrument, fix, regression check) stops it changing code before it has seen the bug.

**After**

1. Keep the app running. Select `debugger`. Ask: `On /board, ticking the first task leaves the wrong checkboxes ticked. Reproduce it in the browser and fix it.`
2. Watch it reproduce the bug before it changes code. Repeat the Before steps yourself.

**Try** Rename the server key in your tool's MCP config only and see what the agent reports.

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step07 after"` (or reset to discard the After run). `git status` should be clean before Step 8.

### Step 8: Diagnostics skill: slow page
**Before:** visible lag on each keystroke and `[` breaks the page; record CPU %, the settle time and the top frame from the trace.\
**After:** no visible lag (settles in about 1 s), `[` is a normal search, CPU % is lower and the old top frame is gone from the top 15.

**Before**

1. Open http://localhost:5280/bulk. Click **Generate 5,000 sample tasks**.
2. Type `sample task 12` into Search, one key at a time. Note how long the count takes to settle.
3. Type `[`, then a few more keys. The count stops updating; there is no error message.

**Copy** `cp -R examples/step08-diagnostics-skill/. ./ && git add -A && git commit -m "lab: step08 copy example"` → `.agents/skills/dotnet-diagnostics/` (`SKILL.md`, `scripts/`), `.claude/skills/dotnet-diagnostics` (symlink) (matrix row 4).

**Read**

- A skill can ship scripts. The agent runs them instead of improvising long commands.
- Each script does one job, uses `set -euo pipefail` and is bounded by time; output goes to `.diag/` (gitignored).
- `counters.sh` names `System.Runtime[cpu-usage,gc-heap-size]`; plain `System.Runtime` on .NET 9+ lacks them.
- `top-frames.py` adds `UNMANAGED_CODE_TIME` to the managed caller, or the ranking is meaningless.

**After**

1. Before any fix, ask the agent to use the skill to measure `/bulk` while you type in Search. Record CPU usage, settle time and the top frames.
2. Ask the agent to fix the page using the measurements.
3. Measure again with the same steps. Compare the numbers.

**Try** Run `top-frames.py --inclusive` on the same trace and compare the ranking.

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step08 after"` (or reset to discard the After run). `git status` should be clean before Step 9.

### Step 9: Diagnostics skill: memory growth
**Before:** GC heap size grows by tens of MB over 20 Dashboard visits and does not drop; record the MB.\
**After:** the same 20 visits leave the heap roughly flat (a few MB), and the second heap dump shows no growth.

**Before**

1. Restart the app. Start `dotnet-counters monitor -n CRUDTasksWithAgent`. Watch the GC heap size.
2. Open http://localhost:5280/dashboard. Move to another page and back 20 times. Tick and untick a task on `/board` a few times.
3. Wait a minute. Does the heap size drop back?

**Copy** Nothing new: the Step 8 skill already has a "Memory growth" section. **Read** it: two heap dumps, the same action between them, compare the types that grew.

**After**

1. Ask the agent to use the skill: take a heap dump, repeat Before step 2, take another, and compare.
2. Ask it to fix the cause it finds. Repeat Before. The heap should level off.

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step09 after"` (or reset to discard the After run). `git status` should be clean before Step 10.

### Step 10: Hooks (guard rails)
**Before:** `git status` shows `tdd-red` edited `src/` and the agent edited `infra/main.bicep`.\
**After:** both edits are blocked with a reason, `git status` shows no `src/` or `infra/` changes, and a syntax error is reported.

**Before**

1. Select `tdd-red`. Ask: `Write a failing test for an empty task title, then make it pass.` Note whether it edits files under `src/`.
2. In the default agent, ask: `Change the App Service SKU in infra/main.bicep to P1v3.` Note whether it edits the file.
3. Reset.

**Copy** `cp -R examples/step10-hooks/. ./ && git add -A && git commit -m "lab: step10 copy example"` → `scripts/hooks/{lib,guard-edits,build-after-edit}.sh`, `.github/hooks/lab-guards.json` + `.github/hooks/scripts` (symlink), `.claude/settings.json`, `.codex/hooks.json`, updated `.agents/agents/tdd-{red,green}.md` (matrix rows 8-9). Enable hooks first (Setup 8).

**Read**

- Instructions ask; hooks enforce. A pre-tool hook can deny a tool call with a reason the agent sees.
- One script per job serves all tools: `--tool copilot|claude|codex` picks the output format; `lib.sh` reads both payload styles.
- `guard-edits.sh` blocks `infra/`, and everything outside `tests/` while `.tdd-red` exists. `tdd-red` now creates the marker; `tdd-green` removes it.
- `build-after-edit.sh` runs after `.cs`/`.razor` edits and reports errors without blocking.

**After**

1. Repeat both Before prompts in new chats. The edits should be blocked with a clear reason.
2. Make a deliberate syntax error through the agent. Its next message should mention the build error.
3. Test a script directly: `echo '{"tool_name":"Edit","tool_input":{"file_path":"infra/main.bicep"}}' | bash scripts/hooks/guard-edits.sh --tool claude`.

**Try** Add `docs/` to the blocked paths in `guard-edits.sh` and ask the agent to edit a doc.

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step10 after"` (or reset to discard the After run). `git status` should be clean before Step 11.

### Step 11: Quality gate
**Before:** the Problems panel and `node scripts/lint-count.mjs` show 24; the agent can add a page that raises it and still finish.\
**After:** the count is 19 or lower, `--check` passes, format verify passes for the files you changed, and the agent will not finish with new warnings.

**Before**

1. Run `node scripts/lint-count.mjs`. Compare the total with `docs/lint-baseline.md`. Run `node scripts/lint-count.mjs --check`.
2. Run `dotnet format CRUDTasksWithAgent.slnx --verify-no-changes --include <changed files>`. Note the exit code. The whole solution fails this check even when finished, so always pass `--include`. Do not run it without `--verify-no-changes`.
3. Ask the agent: `Add an /archive page that lists completed tasks.` Run `node scripts/lint-count.mjs` again. Did the count rise? Did the build fail?
4. Reset.

**Copy** `cp -R examples/step11-quality-gate/. ./ && git add -A && git commit -m "lab: step11 copy example"` → `.agents/skills/quality-gate/SKILL.md`, `.claude/skills/quality-gate` (symlink), `scripts/hooks/quality-gate.sh`, full `.github/hooks/lab-guards.json`, `.claude/settings.json`, `.codex/hooks.json`, `AGENTS.md` (adds "Quality"), and the full `tasks-lab` plugin and marketplace files (matrix rows 4, 8, 11).

**Read**

- The skill is the checklist (build, format verify on changed files, lint count, tests) and the rule: fix new warnings, never suppress them or raise the baseline.
- The stop hook (Copilot `agentStop`, Claude/Codex `Stop`) runs the same checks and returns `decision: block` to keep the agent working.
- `AGENTS.md` points at the skill, so the agent runs it before it tries to stop.
- The plugin now also ships the TDD and debugger agents, both new skills and the hooks.

**After**

1. Ask the agent to promote nullable warnings to errors in `Directory.Build.props`, fix what breaks, and lower `docs/lint-baseline.md` to the new total.
2. Repeat the Before prompt in a new chat. The agent should not finish while the count is above the baseline: look for a second agent turn after it tries to stop.
3. `node scripts/lint-count.mjs --check` passes and `dotnet build` shows no nullable warnings.

**Try** Lower the baseline total by one and watch the stop hook send the agent back to work.

Finish with `dotnet build` and `dotnet test`.

**Commit** Undo any **Try** edits to the example first (`git diff` shows them), then `git add -A && git commit -m "lab: step11 after"`. `git status` should be clean.


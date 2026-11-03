# Workshop: Copilot customization and advanced workflows

Two sessions. In each step you see a problem, build a Copilot primitive, and use it to fix the problem.

For inspiration, browse [github/awesome-copilot](https://github.com/github/awesome-copilot). Write your own files; do not copy them in wholesale.

## Setup

1. Install the .NET 10 SDK, VS Code Insiders with GitHub Copilot, Node.js 22+ and Git.
2. Install the diagnostics tools:
   `for t in dotnet-counters dotnet-trace dotnet-gcdump dotnet-stack; do dotnet tool install -g $t; done`
3. Create your branch: `git switch workshop/baseline && git switch -c lab/<your-name>`.
4. Run the app: `dotnet run --project src/CRUDTasksWithAgent`. Open http://localhost:5280.
5. Run the tests once: `dotnet test`.
6. Commit after each step. Use a short message such as `lab: step 1 AGENTS.md`.
7. Note: task data lives in memory. Restart the app to reset it.

### Session target

Use the **Local** session target in Copilot Chat for this workshop. Agent handoffs and prompt files only work there.

### How each step works

**Symptom**: what to do and what to look for. **Create**: the file to add and what it should cover; you write the content. **Prove it**: run the same prompt or measurement before and after, in a new chat, and compare.

## Session 1: Instructions and agents

### Step 1: Project instructions (`AGENTS.md`)

**Symptom**

1. Open http://localhost:5280/recent. Note what it shows.
2. In Agent mode, ask: `Add a /completed page that lists completed tasks and shows when the page was loaded.`
3. Review the code it writes. Compare it with the rest of the app. Would you approve it?
4. Discard the change: `git restore . && git clean -fd src tests`.

**Create**

`AGENTS.md` at the repo root, under 60 lines. Cover the stack and folder layout, how components get data, how code gets the current time, what every new app-agent tool needs, build and test commands, and folders the agent must not touch without asking.

**Prove it**

Run the same prompt in a new chat. The new page should follow your rules, not the patterns in `/recent`. Then ask the agent to fix `/recent` to match.

### Step 2: File-scoped instructions (`*.instructions.md`)

**Symptom**

1. Open http://localhost:5280/board.
2. Tick the first task. Watch every checkbox after the list reorders.
3. Untick it again. Watch again.
4. Open http://localhost:5280/dashboard. Move between Dashboard and other pages ten times.
5. Ask the agent: `Review Board.razor and Dashboard.razor for Blazor problems.` Note what it misses.

**Create**

`.github/instructions/blazor.instructions.md`. Set `applyTo` so it loads only for Razor files. List the Blazor rules you want every component to follow: rendering lists, subscribing to events, updating UI from other threads, large lists, search inputs, and data access.

Optionally add `.github/instructions/tests.instructions.md` for files under `tests/`.

**Prove it**

1. Open `Board.razor` and repeat the review prompt in a new chat. Check the references list: your instructions file should appear.
2. Ask the agent to add a new component with a list and an event subscription. Check it follows your rules.

Do not fix the bugs yet. Steps 5 and 7 cover them.

### Step 3: Skill vs prompt file (`add-agent-tool`)

**Symptom**

1. Open http://localhost:5280/agentfx. Type `help`. Note the commands the scripted model understands.
2. Ask the agent: `Add an app-agent tool that deletes all completed tasks. Users should be able to type "clear completed".`
3. Check: Does `clear completed` work in `/agentfx`? Is there a test? Run `dotnet test`. Count the files changed.
4. Discard the change: `git restore . && git clean -fd src tests`.

**Create**

1. `.github/prompts/add-agent-tool.prompt.md`. Ask for the tool name and description with `${input:...}` variables.
2. `.github/skills/add-agent-tool/SKILL.md`. Write the full procedure: every file to change, in order, with exact paths, and how to check the result.

Find the files yourself. Start from `src/CRUDTasksWithAgent/Tools/` and `tests/`.

**Prove it**

1. Run the prompt file with `/add-agent-tool`. Check the same things as in the Symptom.
2. Discard. Ask the plain prompt from the Symptom again in a new chat. The skill should load by itself.
3. Write down one difference between a prompt file and a skill.

### Step 4: Read-only reviewer agent (`docs-reviewer`)

**Symptom**

1. Read `docs/architecture.md`.
2. Compare it with the folders and pages in `src/CRUDTasksWithAgent`.
3. Note anything missing or wrong.

**Create**

`.github/agents/docs-reviewer.agent.md`. Give it read and search tools only. Tell it to compare the docs with the code and report drift as a list. It must never edit files.

**Prove it**

1. Select the `docs-reviewer` agent. Ask: `Review docs/architecture.md.`
2. Compare its list with yours.
3. Ask it to fix the doc. It should refuse or have no edit tool.
4. Switch to the default agent and fix the doc using the report.

### Step 5: Package it (plugin)

**Symptom**

Your primitives are spread across `AGENTS.md`, `.github/instructions`, `.github/prompts`, `.github/skills` and `.github/agents`. A teammate on another repo cannot reuse them easily.

**Create**

1. `plugins/tasks-lab/plugin.json` that lists your agents, skills and (later) hooks.
2. `.github/plugin/marketplace.json` that lists the plugin.

Match the format used in [github/awesome-copilot](https://github.com/github/awesome-copilot) (`plugins/` and `.github/plugin/`).

**Prove it**

1. Add your repo as a plugin marketplace in VS Code.
2. Install `tasks-lab` from it in another workspace.
3. Check the agents and skills appear there.

Come back to this step after Session 2 to add the new skill and hooks.

---

## Session 2: Advanced workflows

### Step 6: TDD agents with handoffs (`tdd-red`, `tdd-green`, `tdd-refactor`)

**Symptom**

1. Open http://localhost:5280/. Delete every task.
2. Open http://localhost:5280/dashboard. Read the completion rate.
3. A user also reports that "Week starting" shows the wrong date on some days of the week.
4. Look in `tests/`. Is `TaskStatsCalculator` tested?

**Create**

Three agents in `.github/agents/`:

- `tdd-red.agent.md`: writes one failing test. It may only edit files under `tests/`. It hands off to `tdd-green`.
- `tdd-green.agent.md`: writes the smallest code change that makes the test pass. It hands off to `tdd-refactor`.
- `tdd-refactor.agent.md`: tidies code and tests without changing behaviour. All tests must stay green.

Use the `handoffs` frontmatter so a button appears at the end of each turn.

**Prove it**

1. Select `tdd-red`. Describe one bug from the Symptom.
2. Confirm the new test fails for the right reason.
3. Click the handoff to `tdd-green`. Confirm the test passes.
4. Click the handoff to `tdd-refactor`.
5. Repeat for the second bug.
6. Check the dashboard again.

### Step 7: Debugger agent with Playwright MCP

**Symptom**

1. Open http://localhost:5280/board.
2. Tick the first task. Note which checkboxes look ticked and which tasks are struck through.
3. Reload the page. Compare.

**Create**

1. `.vscode/mcp.json` that starts the Playwright MCP server (`npx @playwright/mcp@latest`). Start it from the MCP view.
2. `.github/agents/debugger.agent.md`. Give it a fixed loop: reproduce, hypothesise, instrument, fix, add a regression check. Give it the Playwright tools so it can drive the browser itself.

**Prove it**

1. Keep the app running.
2. Select `debugger`. Ask: `On /board, ticking the first task leaves the wrong checkboxes ticked. Reproduce it in the browser and fix it.`
3. Watch it reproduce the bug before it changes code.
4. Repeat the Symptom steps yourself.

### Step 8: Diagnostics skill: slow page

**Symptom**

1. Open http://localhost:5280/bulk. Click **Generate 5,000 sample tasks**.
2. Type `sample task 12` into Search, one key at a time. Note the lag.
3. Type `[`. Note what happens.

**Create**

`.github/skills/dotnet-diagnostics/SKILL.md` with a `scripts/` folder. Add one small script per job: find the app's process id (`dotnet-counters ps`); collect counters as JSON; collect a CPU trace in speedscope format; summarise a speedscope file into the top 15 frames; collect and report a GC heap dump (used in Step 9).

Make scripts executable. Use `set -euo pipefail`. Bound every collection by time. The skill should tell the agent which script to run for which symptom.

**Prove it**

1. Before any fix: ask the agent to use the skill to measure `/bulk` while you type in Search. Record CPU usage and the top frames.
2. Ask the agent to fix the page using the measurements.
3. Measure again with the same steps. Compare the numbers.

### Step 9: Diagnostics skill: memory growth

**Symptom**

1. Restart the app.
2. Start `dotnet-counters monitor -n CRUDTasksWithAgent`. Watch the GC heap size.
3. Open http://localhost:5280/dashboard. Move to another page and back 20 times.
4. Tick and untick a task on `/board` a few times.
5. Wait a minute. Does the heap size drop back?

**Create**

Extend `dotnet-diagnostics` if needed. The skill should explain how to take two heap dumps and compare them.

**Prove it**

1. Ask the agent to use the skill: take a heap dump, repeat steps 3 and 4, take another, and compare.
2. Ask it to fix the cause it finds.
3. Repeat the Symptom. The heap should level off.

### Step 10: Hooks (guard rails)

**Symptom**

1. Select `tdd-red`. Ask: `Write a failing test for an empty task title, then make it pass.` Note whether it edits files under `src/`.
2. In the default agent, ask: `Change the App Service SKU in infra/main.bicep to P1v3.` Note whether it edits the file.
3. Discard both changes.

**Create**

1. `.github/hooks/lab-guards.json` and scripts in `.github/hooks/scripts/`.
2. A `preToolUse` hook that blocks edits under `infra/`.
3. A `preToolUse` hook that blocks edits outside `tests/` while a `.tdd-red` marker file exists. Update `tdd-red` to create the marker and `tdd-green` to remove it. Add `.tdd-red` to `.gitignore`.
4. A `postToolUse` hook that runs a quiet build after a `.cs` or `.razor` edit and reports errors without blocking.

Check the current VS Code hooks docs for the input and output format. Test each script by piping sample JSON into it.

**Prove it**

1. Repeat both Symptom prompts in new chats.
2. The edits should be blocked with a clear reason.
3. Make a deliberate syntax error through the agent. The build hook should report it.

Then return to Step 5 and add the diagnostics skill and hooks to your plugin. Finish with `dotnet build` and `dotnet test`.

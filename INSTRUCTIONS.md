# Spec-driven development with GitHub Spec Kit

These instructions take an idea from a vague problem to working, tested code in this repo using [GitHub Spec Kit](https://github.com/github/spec-kit). You check whether the idea is worth building, write a specification, settle the open questions, plan the change, break it into tasks, and only then let the agent write code against the plan.

Spec Kit works with the coding agent of your choice. The examples use **GitHub Copilot CLI**; see [Using another agent](#using-another-agent) for Codex and Claude Code.

## Install

### 1. Prerequisites

| Tool | Why | Check |
| --- | --- | --- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Build, test and run the app | `dotnet --version` (10.x) |
| [Node.js](https://nodejs.org/) 22.20 or later | Repo helper scripts | `node --version` |
| [Git](https://git-scm.com/) | Branch per step so you can compare and roll back | `git --version` |
| [uv](https://docs.astral.sh/uv/getting-started/installation/) | Installs and runs the Spec Kit CLI; it fetches Python 3.11+ automatically | `uv --version` |
| A coding agent | Runs the Spec Kit steps: [Copilot CLI](https://docs.github.com/copilot/how-tos/copilot-cli), [Codex](https://github.com/openai/codex) or [Claude Code](https://code.claude.com/) | `copilot --version` |

### 2. Check the app builds and its tests pass

```bash
dotnet test
dotnet run --project src/CRUDTasksWithAgent   # http://localhost:5280
```

Note what the task list does today; it's the "before" picture.

### 3. Install the Spec Kit CLI

```bash
uv tool install specify-cli
specify version
```

If you'd rather not install it, prefix each `specify` command with `uvx --from specify-cli`.

### 4. Initialise Spec Kit in this repo, on a branch

```bash
git switch -c speckit/my-feature
specify init --here --force --integration copilot --script sh   # Windows: --script ps
```

- `--here` initialises the current folder.
- `--force` is needed because the folder isn't empty. It merges Spec Kit's files into the repo and may overwrite files with the same names, so run it on a branch and review `git status` afterwards.
- It adds `.specify/` (templates, scripts, the constitution) and one skill per step in `.github/skills/speckit-*/`.

### 5. Add the idea-assessment extension

```bash
specify extension add assess
```

This adds the five `speckit-assess-*` skills used in steps 2–6 below.

### 6. Turn on automatic commits

```bash
specify extension add git
uv run --with pyyaml python scripts/configure_speckit_git.py .
```

After this, every step commits itself with a fixed message such as `[Spec Kit] Add specification`, and specify creates a feature branch (`NNN-<feature>`). Nobody has to remember to commit, and you can compare or roll back any step. On another repo, copy `scripts/configure_speckit_git.py` across first. [docs/speckit-setup.md](docs/speckit-setup.md) explains exactly what this changes and why.

### 7. Check your agent can see the skills

Start your agent in the repo folder and ask it:

```text
List the skills available to you whose names start with speckit.
```

You should see the `speckit-*`, `speckit-assess-*` and `speckit-git-*` skills. If not, restart the agent from the repo root.

Commit at this point (`git add -A && git commit -m "Install Spec Kit"`) so the setup is separate from the feature work. From here on, the steps commit themselves.

> **On branch `demo/01-init`, install steps 3–6 are already done.** See [docs/speckit-setup.md](docs/speckit-setup.md) for how it was configured.

## The steps

Run the steps **in your agent's chat, one at a time**, and review the file each one writes before moving on. Each step writes a Markdown file that the next step reads, so you can start a fresh session between steps without losing context. Each step also **commits itself**; glance at `git log --oneline` to confirm, and run `/speckit-git-commit` if one is missing.

> **Want the exact prompts?** [Walkthrough: copy and paste every step](#walkthrough-copy-and-paste-every-step) has a complete, ready-to-paste prompt for each step below, worked through on this repo.

| # | Step (Copilot / Claude) | What it does | What it writes | Why it matters |
| ---: | --- | --- | --- | --- |
| 1 | `/speckit-constitution <principles>` | Records the project's non-negotiable rules, e.g. testing, layering, dependencies, lint. **Run once per project.** | `.specify/memory/constitution.md` | Every later step is checked against these rules, so standards are enforced each time rather than remembered. |
| 2 | `/speckit-assess-intake slug=<name> <idea>` | Captures a rough idea (text, ticket or pointer into the code) as a normalised intake note. | `.specify/assessments/<slug>/intake.md` | Starts from the problem as it actually arrives, before anyone commits to a solution. |
| 3 | `/speckit-assess-research slug=<name>` | Gathers evidence for and against the idea: users, prior art, existing code and data. | `…/<slug>/research.md` | Tests the idea against evidence instead of enthusiasm, and finds existing work such as `docs/backlog.md`. |
| 4 | `/speckit-assess-define slug=<name>` | Defines the problem: who is affected, what hurts, goals, non-goals and success metrics. | `…/<slug>/problem.md` | You can't judge a solution without an agreed problem and a measure of success. |
| 5 | `/speckit-assess-shape slug=<name>` | Sketches solution options, scope, appetite and trade-offs, with no implementation design. | `…/<slug>/concept.md` | Decides how much the idea is worth spending before anyone designs it. |
| 6 | `/speckit-assess-decide slug=<name>` | Gives a verdict, **go**, **needs-clarification** or **kill**, and for a go, a handoff for specify. | `…/<slug>/decision.md` | Only ideas worth building reach delivery. A documented kill is a good outcome. |
| 7 | `/speckit-specify use the handoff summary in .specify/assessments/<slug>/decision.md` | Writes the feature spec: user stories, acceptance scenarios and requirements, with **no implementation detail**. Marks unknowns `[NEEDS CLARIFICATION]`. | `specs/NNN-<feature>/spec.md` and `checklists/requirements.md` | Agrees *what* and *why* before *how*. The spec is the contract the result is checked against. |
| 8 | `/speckit-clarify` | Asks up to five targeted questions about gaps in the spec and writes the answers back into it. | Updates `spec.md` | Ambiguities get resolved by a human now, not guessed by the agent halfway through coding. |
| 9 | `/speckit-plan <tech constraints>` | Designs the change against the existing code and the constitution: research, data model, API contracts and a quickstart. | `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md` | **The approval gate.** Review and approve the plan before any code is written. |
| 10 | `/speckit-checklist` *(optional)* | Generates a custom quality checklist for the feature, e.g. for UX, security or API. | `checklists/<topic>.md` | Turns "is the spec good enough?" into explicit, checkable items. |
| 11 | `/speckit-tasks` | Breaks the plan into small, dependency-ordered tasks, marking which can run in parallel. | `tasks.md` | Small, ordered tasks keep each change reviewable and give the agent a script to follow. |
| 12 | `/speckit-analyze` *(optional)* | Read-only consistency check across the spec, plan and tasks: gaps, contradictions and breaches of the constitution. | A report in chat | Catches drift between documents before it turns into code. |
| 13 | `/speckit-implement` | Works through `tasks.md`, writing code and tests and ticking off tasks. | Source and test changes; updates `tasks.md` | The agent builds against an approved plan instead of improvising. |
| 14 | `/speckit-converge` | Compares the code with the spec, plan and tasks, and adds any unbuilt work to `tasks.md` as new tasks. | Appends to `tasks.md` | Closes the loop. Repeat implement and converge until it reports **Converged**. |

Afterwards, verify outside the agent. The compiler, tests and linter are the real judges:

```bash
dotnet test
node scripts/lint-count.mjs --check
dotnet run --project src/CRUDTasksWithAgent
```

**Optional:** to turn `tasks.md` into GitHub Issues, run `specify extension add github`, then `/speckit-github-taskstoissues`. This needs a GitHub remote and the GitHub MCP server.

## Walkthrough: copy and paste every step

A complete worked example for this repo. Each block is a full prompt: copy it, paste it into your agent's chat, wait for the step to finish, review what it wrote, then move on. The example idea is *"users can't tell what to work on first"*, which leads to backlog item **B1 "Due dates and priority"** in [docs/backlog.md](docs/backlog.md).

- **Codex users:** change the leading `/` to `$` (e.g. `$speckit-constitution`). Everything else is the same.
- **Your own repo:** the steps are the same. Rewrite the constitution for your codebase and replace the idea with your own.

### Before you start

```bash
git switch -c run/what-first      # a throwaway branch; constitution and assess commit before the feature branch exists
copilot                           # or: codex / claude
```

After each step, run `git log --oneline -3` in another terminal. You should see a new `[Spec Kit] …` commit. If one is missing, run `/speckit-git-commit`.

### Step 1: Constitution (once per project)

```text
/speckit-constitution Create the constitution for this .NET 10 Blazor Interactive Server task app. Principles:
1. Every behaviour change ships with xUnit tests in tests/CRUDTasksWithAgent.Tests, at the service, API and app-agent tool levels as relevant.
2. Keep the layering: Model -> TaskService -> minimal API endpoints in Program.cs -> Blazor page. App-agent tools in Tools/TaskCrudTool.cs call TaskService; they never touch the DbContext directly.
3. Any new or changed app-agent tool needs a matching scripted intent in Services/Scripted/ScriptedIntents.cs, with a working Example, so it works offline and appears as a suggested prompt in the chat.
4. Do not add build warnings beyond the baseline in docs/lint-baseline.md; check with node scripts/lint-count.mjs --check.
5. No new NuGet packages without a written justification in the plan.
6. The app must stay runnable offline with no keys (the scripted model is the default).
7. The UI stays Blazor Interactive Server; moving to WebAssembly is out of scope unless a spec says otherwise.
```

**Review:** `.specify/memory/constitution.md`. Every principle should be checkable, not vague.

### Step 2: Assess, intake

```text
/speckit-assess-intake slug=what-first Users say they can't tell what to work on first. The task list is one flat list in creation order, with no way to mark urgency or deadlines. Evidence: three support requests this month asking for "urgent" or "due date" markers, and backlog item B1 "Due dates and priority" in docs/backlog.md. Users are individuals managing their own tasks in this app, through the task list page and the chat agent.
```

**Review:** `.specify/assessments/what-first/intake.md`.

### Step 3: Assess, research

```text
/speckit-assess-research slug=what-first Look for prior art and constraints in this repo: docs/backlog.md (especially B1), the TaskItem model, TaskService, the /api/tasks endpoints in Program.cs, Components/Pages/TaskList.razor, and the app-agent tools in Tools/TaskCrudTool.cs. Note what already exists, what is missing, and anything that would make this harder, such as the in-memory database or the full-object PUT.
```

**Review:** `research.md`. It should cite B1 and the current data model.

### Step 4: Assess, define

```text
/speckit-assess-define slug=what-first Keep the problem focused on individuals deciding what to do next. A good success metric: with 20 or more open tasks, a user can identify their most urgent incomplete task in under 5 seconds, from both the task list and the chat agent.
```

**Review:** `problem.md`: problem, users, goals, non-goals, success metrics.

### Step 5: Assess, shape

```text
/speckit-assess-shape slug=what-first Compare at least two options, e.g. priority only, versus due date plus priority with sort and filter. Keep the appetite small: something an agent can implement and test in about a day. Shared or team tasks, notifications and recurring tasks are out of scope.
```

**Review:** `concept.md`: options, trade-offs, the recommended option and the appetite.

### Step 6: Assess, decide

```text
/speckit-assess-decide slug=what-first
```

**Review:** `decision.md` gives **go**, **needs-clarification** or **kill**. A "go" commits the assessment (`[Spec Kit] Add idea assessment`) when you run the next step. For **needs-clarification**, edit the file it names (e.g. add evidence to `research.md`) and run this step again.

### Step 7: Specify

```text
/speckit-specify Use the handoff summary in .specify/assessments/what-first/decision.md to specify the approved concept.
```

This creates and switches to a feature branch such as `001-what-first`.

**Review:** `specs/001-*/spec.md`: user stories, acceptance scenarios, and `[NEEDS CLARIFICATION]` markers. No implementation detail.

### Step 8: Clarify

```text
/speckit-clarify Where these decisions answer a question, apply them; ask me about anything else:
- Priority levels are Low, Normal and High; new tasks default to Normal.
- Due dates are dates only (no time); "today" uses the server's local date.
- A task is overdue when its due date is before today and it is not complete.
- Default sort: priority (High first), then due date (earliest first, no date last), then title.
- Filters for priority, due before a date, and overdue can be combined.
- The chat agent can set and change priority and due date, through the existing tools plus scripted intents.
- An invalid priority or date in an API request returns 400.
```

**Review:** `spec.md` has a "Clarifications" section, and the `[NEEDS CLARIFICATION]` markers are gone. Answer any extra questions it asks.

### Step 9: Plan

```text
/speckit-plan Use the existing stack only: EF Core in-memory via TasksDbContext, minimal API endpoints in Program.cs, Blazor Interactive Server in Components/Pages/TaskList.razor, app-agent tools in Tools/TaskCrudTool.cs with matching scripted intents, and xUnit tests using Helpers/ApiFactory.cs and Helpers/TestDb.cs. No new packages. Keep the PUT endpoint backwards compatible, and say how the plan handles a PUT that omits fields.
```

**Review and approve before any code is written:** `plan.md`, `research.md`, `data-model.md`, `contracts/` and `quickstart.md`. Check the constitution check passes.

### Step 10: Checklist (optional)

```text
/speckit-checklist Create a checklist for API and UX quality: validation and 400 responses, sort and filter behaviour, keyboard accessibility of the new controls, and offline behaviour of the chat agent's scripted intents.
```

**Review:** `checklists/`. Tick or fix items before tasks.

### Step 11: Tasks

```text
/speckit-tasks Write tests first for each layer (service, API, app-agent tool), then the implementation, then the UI. Keep each task small enough to review on its own, and mark tasks that can run in parallel.
```

**Review:** `tasks.md`. Tasks should be small, ordered, and each traceable to a requirement.

### Step 12: Analyze (optional)

```text
/speckit-analyze
```

**Review:** the report in chat. Fix any contradictions between the spec, plan and tasks before implementing.

### Step 13: Implement

```text
/speckit-implement Work through tasks.md in order. After each phase, run dotnet test and fix failures before continuing. At the end, run node scripts/lint-count.mjs --check and fix any new warnings.
```

**Review:** the diff, and `tasks.md` with tasks ticked off.

### Step 14: Converge

```text
/speckit-converge
```

If it adds tasks, run `/speckit-implement` again, then `/speckit-converge`, until it reports **Converged**.

### Check the result yourself

```bash
dotnet test
node scripts/lint-count.mjs --check
dotnet run --project src/CRUDTasksWithAgent --urls http://localhost:5280
curl "http://localhost:5280/api/tasks"      # tasks now include priority and due date
```

Open http://localhost:5280/ to try the new controls, and http://localhost:5280/agentfx to see the chat's new suggested prompts. Then compare the result with the success metric from step 4.

## Using another agent

The steps, files and order are the same in every agent. Only the setup flag and how you invoke a step differ:

| | Copilot CLI | Codex | Claude Code |
| --- | --- | --- | --- |
| `specify init … --integration` | `copilot` | `codex` (needs the `codex` CLI, or add `--ignore-agent-tools`) | `claude` |
| Skills are installed in | `.github/skills/speckit-*` | `.agents/skills/speckit-*` | `.claude/skills/speckit-*` |
| Invoke a step | `/speckit-specify …` | `$speckit-specify …` | `/speckit-specify …` |
| Switch an initialised repo | `specify integration switch copilot` | `specify integration switch codex` | `specify integration switch claude` |

Run `specify integration list` to see every supported agent.

**On branch `demo/01-init`, Copilot and Codex are both set up**, so Codex users can type `$speckit-…` straight away. To add a second agent to your own repo, see [docs/speckit-setup.md](docs/speckit-setup.md#how-the-two-agents-were-set-up).

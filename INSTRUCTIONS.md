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

### 6. Check your agent can see the skills

Start your agent in the repo folder and ask it:

```text
List the skills available to you whose names start with speckit.
```

You should see the `speckit-*` and `speckit-assess-*` skills. If not, restart the agent from the repo root.

Commit at this point (`git add -A && git commit -m "Install Spec Kit"`) so the setup is separate from the feature work.

## The steps

Run the steps **in your agent's chat, one at a time**, and review the file each one writes before moving on. Each step writes a Markdown file that the next step reads, so you can start a fresh session between steps without losing context. Committing after each step lets you compare and roll back.

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

### An idea to try

```text
/speckit-assess-intake slug=what-first Users say they can't tell what to work on first:
the task list is one flat list in creation order.
```

The research step should find backlog item **B1 "Due dates and priority"** in [docs/backlog.md](docs/backlog.md). Any other backlog item, or a problem from your own work, works just as well.

## Using another agent

The steps, files and order are the same in every agent. Only the setup flag and how you invoke a step differ:

| | Copilot CLI | Codex | Claude Code |
| --- | --- | --- | --- |
| `specify init … --integration` | `copilot` | `codex` (needs the `codex` CLI, or add `--ignore-agent-tools`) | `claude` |
| Skills are installed in | `.github/skills/speckit-*` | `.agents/skills/speckit-*` | `.claude/skills/speckit-*` |
| Invoke a step | `/speckit-specify …` | `$speckit-specify …` | `/speckit-specify …` |
| Switch an initialised repo | `specify integration switch copilot` | `specify integration switch codex` | `specify integration switch claude` |

Run `specify integration list` to see every supported agent.

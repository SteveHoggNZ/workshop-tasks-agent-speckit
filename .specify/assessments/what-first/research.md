# Idea Research: Help users know what to work on first

- **Slug**: what-first
- **Created**: 2026-11-01T19:36:32+13:00
- **Evidence confidence (overall)**: medium (code facts are high; demand evidence is low and unverified)

## Users & Demand

- Three support requests this month asked for "urgent" or "due date" markers — [source: intake.md, as
  stated by the idea author; the tickets themselves are not in the repo] (confidence: low, ASSUMPTION
  until the tickets are seen)
- Backlog item B1 "Due dates and priority" (size M) already describes the same need as a user story —
  [source: docs/backlog.md, B1] (confidence: high that the item exists; it is a planned want, not
  observed behaviour)
- `TaskList.razor` renders tasks in creation order only, with no urgency or date affordance —
  [source: src/CRUDTasksWithAgent/Components/Pages/TaskList.razor; Services/TaskService.cs
  `GetAllTasksAsync` orders by `Id`] (confidence: high)
- No usage data, interviews, or ticket text exist in the repo — [source: repo search for
  support/priority/due/urgent finds only docs/backlog.md and INSTRUCTIONS.md] (confidence: high)

## Prior Art

- **B1 is the in-repo prior art.** It already specifies: `DueDate` and `Priority` on `TaskItem`,
  default priority `Normal`, `GET /api/tasks?sort=priority`, `?dueBefore=`, a UI priority filter with
  overdue marking, and HTTP 400 for invalid priority — [source: docs/backlog.md, B1] (high)
- **Nothing is implemented yet.** `TaskItem` has only `Id`, `Title`, `IsComplete` — [source:
  Models/TaskItem.cs] (high)
- `GET /api/tasks` takes no query parameters; `TaskService` has no filter or sort — [source: Program.cs,
  Services/TaskService.cs] (high)
- B4 (weekly summary) and B5 (triage sub-agent) explicitly depend on B1 — [source: docs/backlog.md] (high)
- The offline scripted model's command table has rows for add, list, complete, rename and delete only;
  none handle priority or due dates — [source: Services/Scripted/ScriptedIntents.cs] (high)
- Prior Spec Kit specs: none exist (no `specs/` directory); this is the first assessment — [source:
  repo listing] (high)
- External products (to-do apps with priority/due dates) were not researched; no URLs were fetched —
  (ASSUMPTION: the feature is a common, well-understood pattern)

## Market & Context

- Users cope today by editing task titles (e.g. prefixing "URGENT") or by relying on creation order —
  ASSUMPTION (confidence: low; inferred from the lack of any alternative in the app)
- Cost of doing nothing: continued support requests and B4/B5 remaining blocked — [source: B4/B5
  dependency notes in docs/backlog.md] (medium)

## Data & Constraints

- **In-memory database.** `Program.cs` uses `UseInMemoryDatabase("TasksDb")`. No migrations are needed
  for new columns, which keeps schema changes cheap, but all data (including due dates and priorities)
  is lost on restart. Demo data is seeded in Development only when empty — [source: Program.cs,
  Services/DemoData.cs] (high)
  - Seed data has no priorities or due dates, so the feature would need seed updates to be demonstrable
    (high).
- **Full-object PUT.** `PUT /api/tasks/{id}` binds a whole `TaskItem` and copies only `Title` and
  `IsComplete`. Adding fields forces a choice: extend the copy, which makes omitted fields in an
  existing client's body reset to defaults (silent data loss), or move to partial update semantics.
  Existing tests send `new TaskItem { Title = "after", IsComplete = true }` — [source: Program.cs;
  tests/CRUDTasksWithAgent.Tests/ApiTests.cs `Put_UpdatesTask`] (high)
- **POST uses a query-string title**, not a JSON body, so due date and priority would either join the
  query string or need a body change. B7 notes this inconsistency separately — [source: Program.cs;
  docs/backlog.md B7] (high)
- **Service layer is thin.** `AddTaskAsync(string title)` accepts only a title, and there is no
  query/filter method, so the service signature must grow — [source: Services/TaskService.cs] (high)
- **App-agent tool** `TaskCrudTool.CreateTaskAsync` creates via `AddTaskAsync(title)` and then
  re-reads the "last" task to set completion; the `UpdateTaskAsync` tool handles only `title` and
  `isComplete`. Priority and due date parameters would need adding to both and to the tool
  registration in `Services/AgentFrameworkProvider.cs` (lines ~51–54) — [source: Tools/TaskCrudTool.cs;
  Services/AgentFrameworkProvider.cs] (high)
- **Scripted intents.** The constitution (principle III) requires a matching scripted intent with a
  working `Example` for any new or changed tool, so offline chat must handle priority and due-date
  commands. Additional constraints in the table:
  - the `complete`/`delete`/`rename` patterns capture a single digit id (`(\d)`), a pre-existing
    limitation that affects ids of 10 or more;
  - `FormatTaskList` reads only `id`, `title` and `isComplete`, so priority and due date would not show
    in the `list` reply without a change — [source: Services/Scripted/ScriptedIntents.cs;
    .specify/memory/constitution.md] (high)
- **Date handling.** "Today" and overdue semantics need a rule (INSTRUCTIONS.md's worked example uses
  the server's local date, dates only). No time-zone handling exists in the app — [source:
  INSTRUCTIONS.md lines ~198–203] (medium)
- **Constitution gates**: tests at service, API and tool levels; layering Model → TaskService →
  endpoints → page; no new NuGet packages without justification; no new build warnings
  (`node scripts/lint-count.mjs --check`); offline-capable; Blazor Interactive Server — [source:
  .specify/memory/constitution.md] (high)
- UI in `TaskList.razor` has no filter or sort controls and only holds a local `tasks` list; filter
  state would be new component state — [source: TaskList.razor] (high)

## Evidence Against the Idea

- The demand signal is thin and unverified: three requests, not in the repo, and no data on how many
  users are affected — [source: intake.md] (confidence: low)
- Not every user may want a full due-date/priority model; a simpler marker (e.g. one "urgent" flag)
  would also match the stated requests ("urgent" or "due date") — ASSUMPTION
- Because data is in-memory, priorities and due dates disappear on every restart, which weakens the
  value of date-based planning until persistence (B2) exists — [source: Program.cs; docs/backlog.md B2]
  (high)
- The full-object PUT contract means a naive implementation can silently erase priority/due date for
  existing clients — [source: Program.cs PUT handler] (high)
- Changing the default list order could surprise users who rely on creation order; today's order is
  the only signal they have — ASSUMPTION
- Scope risk: B1 touches Model, Service, API, UI, Tool, Intents and Tests (a size-M item across every
  layer), so the "small appetite" is sensitive to how much of the B1 acceptance criteria is taken —
  [source: docs/backlog.md B1 touchpoints] (medium)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: Can the three support requests be seen? What exactly did users ask for, and was
  it a due date, an urgent marker, or both?]
- [NEEDS CLARIFICATION: How many users and tasks are typical? The metric in INSTRUCTIONS.md assumes 20+
  open tasks; there is no data to confirm it.]
- [NEEDS CLARIFICATION: Should PUT stay a full-object replacement or become partial for the new fields?]
- [NEEDS CLARIFICATION: Is the loss of data on restart acceptable for this feature, or does it depend
  on B2?]
- [NEEDS CLARIFICATION: Which date rule applies for "today" (server local date vs. user's time zone)?]
- [NEEDS CLARIFICATION: Should creation order remain the default sort, or become priority-first?]
- [NEEDS CLARIFICATION: Do competitor to-do apps suggest a different minimal solution? Not researched.]

## Sources

- Repository files (read-only): `docs/backlog.md`, `src/CRUDTasksWithAgent/Models/TaskItem.cs`,
  `Services/TaskService.cs`, `Program.cs`, `Components/Pages/TaskList.razor`, `Tools/TaskCrudTool.cs`,
  `Services/Scripted/ScriptedIntents.cs`, `Services/AgentFrameworkProvider.cs`, `Services/DemoData.cs`,
  `tests/CRUDTasksWithAgent.Tests/ApiTests.cs`, `INSTRUCTIONS.md`, `.specify/memory/constitution.md`
- No external URLs were fetched.

# Backlog for spec-driven labs

Ten small features for the spec-driven development labs. Each is sized for 1–2 hours of agent-assisted work: write a spec first, then let your coding agent implement it against the spec.

Sizes: **S** ≈ 1 hour, **M** ≈ 1.5–2 hours.

Layer key used in the touchpoints:

- **Model** — `src/CRUDTasksWithAgent/Models/`
- **Service** — `Services/TaskService.cs`
- **API** — endpoints in `Program.cs`
- **UI** — `Components/Pages/TaskList.razor`
- **Tool** — app-agent tools in `Tools/TaskCrudTool.cs` and their registration in `Services/AgentFrameworkProvider.cs`
- **Intents** — the scripted model's command table, `Services/Scripted/ScriptedIntents.cs`
- **Tests** — `tests/CRUDTasksWithAgent.Tests/`

Whenever a feature adds an app-agent tool, add a matching row to the scripted intents table so it can be exercised offline.

---

## B1. Due dates and priority

**Status:** Delivered in `specs/001-due-date-priority`. Note: the scripted `complete`, `rename` and `delete` intents still accept single-digit ids only.

**Size:** M

**User story:** As a user, I want to give tasks a due date and a priority, and filter and sort by them, so that I can see what matters first.

**Acceptance criteria**

1. Given a new task with no due date or priority, when it is created, then it has no due date and the priority `Normal`.
2. Given tasks with different priorities, when I request `GET /api/tasks?sort=priority`, then high-priority tasks come first.
3. Given tasks with due dates, when I request `GET /api/tasks?dueBefore=2026-01-31`, then only tasks due on or before that date are returned.
4. Given the task list page, when I choose a priority filter, then only tasks with that priority are shown, with overdue incomplete tasks marked.
5. Given an invalid priority value in a request, when it is sent, then the API returns 400.

**Touchpoints:** Model (`TaskItem`: `DueDate`, `Priority`), Service (filter and sort), API (query parameters, PUT body), UI (inputs, filter, overdue marker), Tool (`CreateTaskAsync` and `UpdateTaskAsync` parameters), Intents (e.g. `add task <title> due <date>`), Tests.

---

## B2. Tags and move to SQLite

**Size:** M

**User story:** As a user, I want to tag tasks and have them survive an app restart, so that I can manage my work over time.

**Acceptance criteria**

1. Given the app is configured with a SQLite connection string, when I create a task and restart the app, then the task is still there.
2. Given a task, when I add the tag `home`, then `GET /api/tasks?tag=home` returns it.
3. Given a tag that is not used by any task, when I filter by it, then an empty list is returned, not an error.
4. Given Development mode with an empty database, when the app starts, then demo data is still seeded once and not duplicated on the next start.
5. Given the automated tests, when they run, then they use an isolated database and do not touch the developer's SQLite file.

**Touchpoints:** Model (`Tag` or a tags collection, `TasksDbContext` mapping, migration or `EnsureCreated`), Service, API (`tag` query parameter), UI (tag input and filter), `Program.cs` (replace `UseInMemoryDatabase`), `appsettings*.json` (connection string), `Services/DemoData.cs`, Tests (`Helpers/TestDb.cs`, `Helpers/ApiFactory.cs`).

---

## B3. Human approval before the app agent deletes tasks

**Size:** M

**User story:** As a user, I want the app agent to ask me before it deletes a task, so that a misunderstanding cannot destroy my data.

**Acceptance criteria**

1. Given the app agent is asked to delete a task, when it decides to call the delete tool, then the task is not deleted until I approve.
2. Given a pending delete approval, when I approve it, then the task is deleted and the agent confirms.
3. Given a pending delete approval, when I reject it, then the task remains and the agent says so.
4. Given any non-destructive request such as listing or adding tasks, when the agent runs, then no approval is requested.
5. Given the scripted model, when I say `delete 2`, then the same approval flow applies.

**Touchpoints:** Tool/agent registration (wrap the delete function as an approval-required function using the Agent Framework's approval support), UI (`AgentFrameworkAgent.razor`: show the pending request with Approve and Reject buttons and send the response back to the agent), Intents (the `delete` row must keep working through the approval round trip), Tests (scripted model with an approval step).

---

## B4. Weekly summary tool

**Size:** S

**User story:** As a user, I want to ask the app agent for a summary of my week, so that I can see what I finished and what is still open.

**Acceptance criteria**

1. Given tasks completed in the last 7 days, when I ask for a weekly summary, then the reply lists them.
2. Given incomplete tasks due in the next 7 days, when I ask, then the reply lists them separately as upcoming.
3. Given no matching tasks, when I ask, then the reply says there is nothing to report rather than failing.
4. Given the tool is called directly, then it returns the same data without calling a model.

**Touchpoints:** Model (needs a `CompletedAt` timestamp, and due dates from B1 if upcoming tasks are included), Service (query for the week), Tool (new `WeeklySummaryAsync` function and registration), Intents (e.g. `weekly summary`, with a summary formatter), Tests. Depends on B1 for the upcoming section.

---

## B5. Triage sub-agent

**Size:** M

**User story:** As a user, I want a triage helper that proposes a priority and a clearer title for new tasks, so that my list stays consistent.

**Acceptance criteria**

1. Given a vague task title, when I ask the app agent to triage it, then it returns a suggested title and priority without changing the task.
2. Given a suggestion, when I confirm it, then the task is updated through the normal update tool.
3. Given the main agent, when it needs triage, then it delegates to the triage sub-agent exposed as a tool rather than doing it inline.
4. Given the scripted model, when I say `triage <id>`, then a deterministic suggestion is returned so the flow works offline.

**Touchpoints:** Agent setup in `Services/AgentFrameworkProvider.cs` (a second agent with its own instructions, exposed to the main agent as a function), Tool (read-only helper for the sub-agent), Intents (`triage <id>`), Tests. Depends on B1 for priority.

---

## B6. Expose tasks over an MCP server at `/mcp`

**Size:** M

**User story:** As a developer, I want my coding agent to read and manage tasks through an MCP server, so that external agents can work with the same data as the app.

**Acceptance criteria**

1. Given the app is running, when an MCP client connects to `/mcp`, then it can list the available tools.
2. Given the server exposes only list, read, create and complete, when the tools are listed, then there is no update or delete tool.
3. Given a task, when a client calls the complete tool with its id, then the task is marked complete and visible in the UI.
4. Given an unknown id, when a client calls read or complete, then it gets a clear tool error, not a server crash.
5. Given the automated tests, when they call the MCP endpoint in-process, then the four tools behave as above.

**Touchpoints:** `Program.cs` (register the C# MCP SDK's HTTP transport and map `/mcp`), a new MCP tools class that calls `TaskService`, Tests (in-process client via the test factory). The REST API and app agent are unchanged.

---

## B7. POST body consistency and title validation

**Size:** S

**User story:** As an API consumer, I want to create tasks with a JSON body like every other write, and get clear errors for bad titles, so that the API is predictable.

**Acceptance criteria**

1. Given `POST /api/tasks` with a JSON body `{"title":"Buy milk"}`, when it is sent, then a task is created and the response is 201 with a `Location` header.
2. Given a missing, empty or whitespace-only title, when it is sent to POST or PUT, then the response is 400 with a problem-details body naming the title.
3. Given a title longer than 200 characters, when it is sent to POST or PUT, then the response is 400.
4. Given the OpenAPI document, when I read it, then POST shows a request body and no `title` query parameter.
5. Given the existing UI and app-agent paths, when they create tasks, then they keep working.

**Touchpoints:** API (`Program.cs` POST and PUT), Service or a small validator, OpenAPI description, Tests (`ApiTests.cs`, which currently uses the query string for POST and must be updated). Decide in the spec whether the query-string form is dropped or kept as a deprecated alternative.

---

## B8. Search

**Size:** S

**User story:** As a user, I want to search tasks by text, so that I can find one in a long list.

**Acceptance criteria**

1. Given tasks with different titles, when I request `GET /api/tasks?q=milk`, then only tasks whose title contains `milk` are returned.
2. Given a search term in a different case, when I search, then the match is case-insensitive.
3. Given no matches, when I search, then an empty list and status 200 are returned.
4. Given the task list page, when I type in the search box, then the list filters as I type.
5. Given the scripted model, when I say `find <text>`, then matching tasks are listed.

**Touchpoints:** Service, API (`q` parameter), UI (search box), Tool (optional `search` parameter on `ReadTasksAsync`), Intents (`find <text>`), Tests.

---

## B9. Bulk complete

**Size:** S

**User story:** As a user, I want to complete several tasks at once, so that I can tidy up quickly.

**Acceptance criteria**

1. Given several tasks selected in the UI, when I click "Complete selected", then all are marked complete.
2. Given `POST /api/tasks/complete` with a list of ids, when it is sent, then those tasks are completed and the response lists the ids that were updated.
3. Given a list that contains an unknown id, when it is sent, then known tasks are still completed and the unknown ids are reported.
4. Given an empty list, when it is sent, then the response is 400.
5. Given the scripted model, when I say `complete 1, 2, 3`, then all three are completed.

**Touchpoints:** Service (single-save bulk operation), API (new endpoint), UI (checkboxes and button), Tool (new function taking several ids), Intents (extend or add a row for lists of ids; the existing `complete` row matches a single digit), Tests.

---

## B10. Audit log

**Size:** M

**User story:** As a user, I want a record of who changed what, including changes made by the app agent, so that I can trust and review its actions.

**Acceptance criteria**

1. Given any create, update or delete, when it succeeds, then an audit entry records the action, task id, time and source (`ui`, `api` or `agent`).
2. Given the app agent deletes a task, when I view the log, then the entry shows source `agent`.
3. Given `GET /api/audit`, when it is called, then the newest entries come first and can be limited with a `limit` parameter.
4. Given a failed operation such as an unknown id, when it happens, then no audit entry is written.
5. Given the task list page, when I open the "Activity" panel, then I see the latest 20 entries.

**Touchpoints:** Model (`AuditEntry` and `TasksDbContext` set), Service (record entries inside `TaskService` with a source passed by the caller), API (`/api/audit`), UI (activity panel), Tool (pass source `agent`), Tests. Works with the in-memory database or with SQLite from B2.

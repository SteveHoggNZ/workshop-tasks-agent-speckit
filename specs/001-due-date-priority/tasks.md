---

description: "Task list for Due Dates and Priority for Tasks"
---

# Tasks: Due Dates and Priority for Tasks

**Input**: Design documents from `/specs/001-due-date-priority/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required (constitution principle I; spec FR-015). Within each story, tests are written first, per layer (service, API, app-agent tool), and must fail before the implementation. UI has no component-test framework; it is verified by the manual steps in quickstart.md.

**Organization**: Grouped by user story. `[P]` = can run in parallel (different files, no dependency on an incomplete task).

## Format: `- [ ] [ID] [P?] [Story] Description with file path`

## Path Conventions

- App: `src/CRUDTasksWithAgent/`
- Tests: `tests/CRUDTasksWithAgent.Tests/`

---

## Phase 1: Setup

- [X] T001 Record the baseline: run `dotnet test tests/CRUDTasksWithAgent.Tests` and `node scripts/lint-count.mjs --check`; confirm all pass and the warning count is 19 (no files changed)
- [X] T002 [P] Add a fixed-time `TimeProvider` test helper (settable local date) in `tests/CRUDTasksWithAgent.Tests/Helpers/FixedTimeProvider.cs`

---

## Phase 2: Foundational (blocks all user stories)

**Purpose**: Core types shared by every story. No behaviour change yet beyond new defaults.

- [X] T003 [P] Create `TaskPriority` enum in `src/CRUDTasksWithAgent/Models/TaskPriority.cs`: `Low = 0`, `Normal = 1`, `High = 2`; custom `JsonConverter` writing the name and reading names case-insensitively, rejecting numbers and unknown names
- [X] T004 Add `Priority` (`TaskPriority`, default `Normal`) and `DueDate` (`DateOnly?`, default null) to `src/CRUDTasksWithAgent/Models/TaskItem.cs` (depends on T003)
- [X] T005 [P] Create `TaskInputParser` in `src/CRUDTasksWithAgent/Services/TaskInputParser.cs`: `TryParsePriority` (names only, case-insensitive, invariant) and `TryParseDueDate` (`yyyy-MM-dd`, invariant culture, real calendar dates only) (depends on T003)
- [X] T006 [P] Create `TaskQuery` options type in `src/CRUDTasksWithAgent/Services/TaskQuery.cs`: `Priority` (`TaskPriority?`), `DueOnOrBefore` (`DateOnly?`), `OverdueOnly` (bool), `UrgencyOrder` (bool) (depends on T003)
- [X] T007 Register `TimeProvider.System` as a singleton in `src/CRUDTasksWithAgent/Program.cs` and add an optional `TimeProvider` constructor parameter (default `TimeProvider.System`) to `src/CRUDTasksWithAgent/Services/TaskService.cs`, so existing `new TaskService(db)` callers still compile (depends on T004)
- [X] T008 Run `dotnet test tests/CRUDTasksWithAgent.Tests` and `node scripts/lint-count.mjs --check`; existing tests still pass and warnings stay at the baseline (depends on T003-T007)

**Checkpoint**: Foundation ready.

---

## Phase 3: User Story 1 - See the most urgent task first (Priority: P1) 🎯 MVP

**Goal**: The task list page shows tasks in urgency order, marks overdue tasks, and lets users set priority and due date.

**Independent Test**: With the app running (demo data), the most urgent incomplete task is first, overdue tasks are marked, and priority/due date can be set and cleared on the page (quickstart.md section 3, steps 1-4).

### Tests for User Story 1 (write first; must fail)

- [X] T009 [P] [US1] Service tests in `tests/CRUDTasksWithAgent.Tests/TaskServiceTests.cs`: new task defaults to `Normal` and no due date (FR-002); `AddTaskAsync` with priority and due date stores them; update sets and clears priority/due date; `GetTasksAsync` default order is incomplete first, then priority High to Low, then due date ascending with none last, then title (case-insensitive), then Id (FR-003); completed group ordered the same way
- [X] T010 [P] [US1] Service tests in `tests/CRUDTasksWithAgent.Tests/TaskServiceTests.cs` using `FixedTimeProvider`: overdue is true only when incomplete and due before today; due today is not overdue; completed tasks are never overdue (FR-004, FR-005); add a case where today rolls forward one day

### Implementation for User Story 1

- [X] T011 [US1] In `src/CRUDTasksWithAgent/Services/TaskService.cs`: extend `AddTaskAsync` with optional `priority` and `dueDate`; extend `UpdateTaskAsync`/add a method to set priority and due date (including clearing due date) without disturbing Title/IsComplete
- [X] T012 [US1] In `src/CRUDTasksWithAgent/Services/TaskService.cs`: add `GetTasksAsync(TaskQuery)` returning urgency order per data-model.md (ordering applied in memory; `UrgencyOrder = false` returns Id order); keep `GetAllTasksAsync()` unchanged (depends on T011)
- [X] T013 [US1] In `src/CRUDTasksWithAgent/Services/TaskService.cs`: add `IsOverdue(TaskItem)` using the injected `TimeProvider` local date (depends on T011)
- [X] T014 [P] [US1] Update `src/CRUDTasksWithAgent/Services/DemoData.cs` to seed mixed priorities and due dates relative to today, including at least one overdue incomplete task (FR-016); keep it Development-only
- [X] T015 [US1] Update `src/CRUDTasksWithAgent/Components/Pages/TaskList.razor` to load via `GetTasksAsync` (urgency order) and show each task's priority badge, due date and a text "Overdue" marker for overdue incomplete tasks (depends on T012, T013)
- [X] T016 [US1] In `src/CRUDTasksWithAgent/Components/Pages/TaskList.razor`: add priority select and optional date input to the add form (default Normal, no date), reload after add (depends on T015)
- [X] T017 [US1] In `src/CRUDTasksWithAgent/Components/Pages/TaskList.razor`: allow changing priority and due date on existing tasks and a "clear date" control; list re-sorts after each change; give controls accessible labels and keyboard operability (depends on T016)
- [X] T018 [US1] Run the US1 service tests, then quickstart.md section 3 steps 1-4 manually (depends on T009-T017)

**Checkpoint**: Page delivers the MVP.

---

## Phase 4: User Story 4 - Other clients can read and set urgency safely (Priority: P2)

**Goal**: The HTTP API exposes priority and due date, sorts, validates (400), and does not wipe values on PUT. Placed before US2/US3 because US3 and US2 filtering reuse the validated parsing and query model.

**Independent Test**: quickstart.md section 5.

### Tests for User Story 4 (write first; must fail)

- [X] T019 [P] [US4] API tests in `tests/CRUDTasksWithAgent.Tests/ApiTests.cs`: POST with `priority` and `dueDate` returns 201 with both fields; defaults are `Normal`/null; priority accepted in any letter case and returned canonical; JSON uses string priority and `yyyy-MM-dd` date
- [X] T020 [P] [US4] API tests in `tests/CRUDTasksWithAgent.Tests/ApiTests.cs`: invalid priority (unknown name, number) and invalid date (bad format, `2026-02-30`) return 400 with a body naming the field on POST, PUT and GET query params, and change nothing (FR-012)
- [X] T021 [P] [US4] API tests in `tests/CRUDTasksWithAgent.Tests/ApiTests.cs`: PUT omitting `priority`/`dueDate` leaves them unchanged; PUT with `"dueDate": null` clears it; PUT with values sets them; existing PUT test sending only Title/IsComplete still passes (FR-013, SC-005)
- [X] T022 [P] [US4] API tests in `tests/CRUDTasksWithAgent.Tests/ApiTests.cs`: `GET /api/tasks?sort=priority` returns urgency order; without `sort` returns Id order (FR-011)

### Implementation for User Story 4

- [X] T023 [P] [US4] Create `UpdateTaskRequest` in `src/CRUDTasksWithAgent/Models/UpdateTaskRequest.cs`: `Title`, `IsComplete`, `Priority` (`TaskPriority?`), `DueDate` (`DateOnly?`) whose setter records presence (non-serialized `DueDateSpecified` flag) so omitted differs from explicit null
- [X] T024 [US4] In `src/CRUDTasksWithAgent/Program.cs`: POST accepts optional `priority` and `dueDate` query params, validates via `TaskInputParser`, returns 400 problem on invalid, 201 with the task otherwise (depends on T011, T019, T020)
- [X] T025 [US4] In `src/CRUDTasksWithAgent/Program.cs`: PUT binds `UpdateTaskRequest`; copies Title/IsComplete as before, applies priority only when supplied, due date only when `DueDateSpecified` (null clears); invalid values yield 400 and leave the task unchanged; 404 for unknown id (depends on T023, T011)
- [X] T026 [US4] In `src/CRUDTasksWithAgent/Program.cs`: GET `/api/tasks` accepts `sort=priority` (urgency order via `GetTasksAsync`); invalid `sort` returns 400; default stays Id order (depends on T012, T022)
- [X] T027 [US4] Run the US4 API tests and quickstart.md section 5 (depends on T019-T026)

**Checkpoint**: API safe and complete except filters (US3).

---

## Phase 5: User Story 2 - Manage urgency with the chat agent (Priority: P1)

**Goal**: The offline chat agent creates/updates tasks with priority and due date, lists them in urgency order with both fields, and answers "show overdue".

**Independent Test**: quickstart.md section 4, with no keys or network.

### Tests for User Story 2 (write first; must fail)

- [X] T028 [P] [US2] Tool tests in `tests/CRUDTasksWithAgent.Tests/TaskCrudToolTests.cs`: `CreateTaskAsync` with priority/dueDate stores them; update sets priority, sets dueDate, `clearDueDate` clears it; works for ids of 10 or more; invalid priority/date returns a clear message and changes nothing; `dueDate` with `clearDueDate: true` is rejected
- [X] T029 [P] [US2] Tool tests in `tests/CRUDTasksWithAgent.Tests/TaskCrudToolTests.cs`: `ReadTasksAsync` returns urgency order including priority and due date; `overdueOnly: true` returns only overdue incomplete tasks, using `FixedTimeProvider`
- [X] T030 [P] [US2] Scripted-model tests in `tests/CRUDTasksWithAgent.Tests/ScriptedModelTests.cs`: `add task X priority high due 2026-12-01` routes to CreateTask with both args; `priority 12 low`, `due 12 2026-12-01`, `clear due 12`, `show overdue` route to the right tool and arguments; `add task Buy milk` unchanged; list and overdue replies show priority and due date; empty overdue reply is `Nothing is overdue.`; existing `Suggestions_EachExampleRoutesToItsOwnTool` still passes with the new rows

### Implementation for User Story 2

- [X] T031 [US2] In `src/CRUDTasksWithAgent/Tools/TaskCrudTool.cs`: add optional `priority` and `dueDate` (string) to `CreateTaskAsync`; parse with `TaskInputParser`; call `TaskService` only; use the task returned by the service instead of re-reading "last" (depends on T011, T028)
- [X] T032 [US2] In `src/CRUDTasksWithAgent/Tools/TaskCrudTool.cs`: add optional `priority`, `dueDate` and `clearDueDate` to `UpdateTaskAsync`; clear messages for invalid input and for `dueDate` + `clearDueDate` (depends on T031)
- [X] T033 [US2] In `src/CRUDTasksWithAgent/Tools/TaskCrudTool.cs`: add optional `overdueOnly` to `ReadTasksAsync`; return urgency order with priority and due date in the result (depends on T012, T013, T029)
- [X] T034 [US2] Update the tool parameter descriptions/registration check in `src/CRUDTasksWithAgent/Services/AgentFrameworkProvider.cs` only if `AIFunctionFactory.Create` needs changes for the new parameters (otherwise no edit) (depends on T031-T033)
- [X] T035 [US2] In `src/CRUDTasksWithAgent/Services/Scripted/ScriptedIntents.cs`: change the `add` row to accept optional `priority <level>` and `due <yyyy-mm-dd>` suffixes, with an Example showing both (depends on T031, T030)
- [X] T036 [US2] In `src/CRUDTasksWithAgent/Services/Scripted/ScriptedIntents.cs`: add rows `priority` (`priority <id> <level>`), `due` (`due <id> <yyyy-mm-dd>`), `clear-due` (`clear due <id>`), each with `\d+` ids and a working Example (depends on T032, T035)
- [X] T037 [US2] In `src/CRUDTasksWithAgent/Services/Scripted/ScriptedIntents.cs`: add row `overdue` (`show overdue`, `ReadTasks { overdueOnly: true }`, Example `show overdue`); update `FormatTaskList` to print `<id>. [ ] <title> (<Priority>, due <date>)` and return `Nothing is overdue.` for an empty overdue result (depends on T033, T036)
- [X] T038 [US2] Run the US2 tests and quickstart.md section 4 offline (depends on T028-T037)

**Checkpoint**: Page and chat both answer "what first".

---

## Phase 6: User Story 3 - Filter to what matters now (Priority: P2)

**Goal**: Combinable filters by priority, due on or before a date, and overdue on the page and API.

**Independent Test**: quickstart.md section 3 step 5 and section 5 filter requests.

### Tests for User Story 3 (write first; must fail)

- [X] T039 [P] [US3] Service tests in `tests/CRUDTasksWithAgent.Tests/TaskServiceTests.cs`: each filter alone; due-on-or-before is inclusive and excludes tasks with no date; overdue filter excludes completed; all three combined with AND; no match returns an empty list
- [X] T040 [P] [US3] API tests in `tests/CRUDTasksWithAgent.Tests/ApiTests.cs`: `priority`, `dueBefore`, `overdue` params alone and combined; no matches returns 200 `[]`; invalid `priority`, `dueBefore` or `overdue` returns 400

### Implementation for User Story 3

- [X] T041 [US3] In `src/CRUDTasksWithAgent/Services/TaskService.cs`: apply `TaskQuery` filters (priority, due on or before, overdue) in `GetTasksAsync` before ordering (depends on T012, T039)
- [X] T042 [US3] In `src/CRUDTasksWithAgent/Program.cs`: GET `/api/tasks` accepts `priority`, `dueBefore`, `overdue`, validated via `TaskInputParser`, invalid gives 400 (depends on T026, T041, T040)
- [X] T043 [US3] In `src/CRUDTasksWithAgent/Components/Pages/TaskList.razor`: add filter controls (priority select, due-on-or-before date, overdue-only checkbox) with labels, keyboard operable, combinable; empty-result message with a "clear filters" control (depends on T017, T041)
- [X] T044 [US3] Run the US3 tests and quickstart.md filter steps (depends on T039-T043)

**Checkpoint**: All stories complete.

---

## Phase 7: Polish & Cross-Cutting

- [X] T045 Run `dotnet test tests/CRUDTasksWithAgent.Tests` (all pass) and `node scripts/lint-count.mjs --check` (no more than 19 warnings; fix any new warnings rather than raising the baseline)
- [X] T046 [P] Confirm no new NuGet packages were added by diffing both `.csproj` files (constitution principle V)
- [X] T047 [P] Update `docs/backlog.md` to mark B1 delivered and note any follow-ups (e.g., single-digit ids in `complete`/`rename`/`delete` scripted rows)
- [X] T048 Walk through all of quickstart.md end to end, including the 20-task 5-second checks for page and chat (SC-001, SC-002)
- [X] T049 Review `checklists/api-ux.md` findings and resolve or record each open item

---

## Dependencies & Execution Order

- Phase 1 then Phase 2 first. Within Phase 2: T003 then T004/T005/T006 (T005 and T006 parallel), T007, T008.
- **US1** (P1) needs Phase 2. **US4** needs T011/T012 from US1. **US2** needs T011-T013 from US1 and `TaskInputParser`. **US3** needs T012 and US1 page (T017) and US4's GET (T026).
- Suggested order: US1 -> US4 -> US2 -> US3. US2 and US4 can run in parallel after US1's service work (T011-T013), as they touch different files except `Program.cs` (US4) vs `Tools`/`ScriptedIntents` (US2).
- Tests in each story are written and seen to fail before that story's implementation tasks.

## Parallel Examples

```text
Phase 2:  T003 first, then T005 and T006 together
US1 tests:  T009 and T010 (same file: write in sequence if done by one person), with T014 (DemoData)
US4 tests:  T019 T020 T021 T022 (same file, separate cases) alongside T023 (UpdateTaskRequest)
After T013:  US2 (Tools, ScriptedIntents) and US4 (Program.cs) in parallel
US2 tests:  T028 + T029 + T030 are in three different files, so run together
```

## Implementation Strategy

- **MVP**: Phases 1-3 (US1): users see and set urgency on the page.
- **Then**: US4 (protects data from legacy PUT), US2 (chat parity, required by the success metric), then US3 (filters).
- Commit after each task or logical group; each task is small enough to review alone.

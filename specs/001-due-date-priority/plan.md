# Implementation Plan: Due Dates and Priority for Tasks

**Branch**: `001-due-date-priority` | **Date**: 2026-11-01 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-due-date-priority/spec.md`

## Summary

Give each task a priority (Low, Normal, High; default Normal) and an optional date-only due date, so users
can see what to do first on the task list page and in the chat agent. The list defaults to most-urgent-first
(incomplete before completed, then priority, then due date, then title), overdue incomplete tasks are marked,
and the page and API can filter by priority, due-before date and overdue. The chat agent sets, changes and
shows both values through its existing tools, plus a scripted "overdue" command, all working offline.

Technical approach (existing stack only, no new packages): extend `TaskItem` with `Priority` and `DueDate`;
add filter/sort and a clock (`TimeProvider`) to `TaskService`; replace the full-object PUT binding with a
request type that can tell "omitted" from "null" for the due date; extend `TaskCrudTool` parameters and the
`ScriptedIntents` table; update `TaskList.razor`. See [research.md](research.md) for the decisions.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`)

**Primary Dependencies**: ASP.NET Core minimal APIs, Blazor Interactive Server, EF Core InMemory provider,
Microsoft Agents AI / `Microsoft.Extensions.AI` (existing). No new packages.

**Storage**: EF Core in-memory database via `TasksDbContext` (unchanged; data is lost on restart)

**Testing**: xUnit 2.9 in `tests/CRUDTasksWithAgent.Tests` (service, API via `WebApplicationFactory`, tool,
scripted model)

**Target Platform**: Server-hosted web app (Azure App Service); runs offline locally with the scripted model

**Project Type**: web-application (single Blazor Interactive Server project plus a test project)

**Performance Goals**: Not a concern at this scale. Filters run in the store, sorting in memory; the 5-second
success metrics are human-scan times, not latency.

**Constraints**: Offline with no keys (scripted model default); no new build warnings beyond the baseline of
19 in `docs/lint-baseline.md`; layering Model -> TaskService -> endpoints -> page; tools call `TaskService` only.

**Scale/Scope**: Single user's personal list; roughly 20 to a few hundred tasks.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Gate | Status |
|-----------|------|--------|
| I. Behaviour changes require layered tests | Service, API, tool and scripted-model tests planned for every behaviour | Pass |
| II. Preserve layering | Page, endpoints and tools all call `TaskService`; tools never touch `TasksDbContext` | Pass |
| III. App-agent tools offline-discoverable | Each changed tool gets a scripted intent with a working `Example` (add, priority, due, clear due, overdue), all in `Suggestions` | Pass |
| IV. Warnings must not increase | Invariant-culture parsing, no unused members; verify with `node scripts/lint-count.mjs --check` | Pass (verify at implement) |
| V. Dependencies need justification | No new NuGet packages; `TimeProvider`, `DateOnly`, `System.Text.Json` converters are in the framework | Pass |
| VI. Offline operation | Scripted model stays default; no new network dependencies | Pass |
| VII. Blazor Interactive Server | `TaskList.razor` stays `InteractiveServer` | Pass |

Post-design re-check: all gates still pass; no complexity violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-due-date-priority/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── tasks-api.md
│   └── agent-tools-and-intents.md
└── tasks.md             # Created by /speckit-tasks, not by this command
```

### Source Code (repository root)

```text
src/CRUDTasksWithAgent/
├── Models/
│   ├── TaskItem.cs                 # + Priority, DueDate
│   ├── TaskPriority.cs             # NEW: enum + JSON converter (names only, case-insensitive)
│   └── UpdateTaskRequest.cs        # NEW: PUT body; tells omitted DueDate from explicit null
├── Services/
│   ├── TaskService.cs              # + query/sort, clock, priority/due on add and update
│   ├── TaskQuery.cs                # NEW: filter/sort options
│   ├── TaskInputParser.cs          # NEW: shared priority/date parsing and validation
│   ├── DemoData.cs                 # + priorities and relative due dates (incl. one overdue)
│   └── Scripted/ScriptedIntents.cs # changed add row; new priority, due, clear-due, overdue rows
├── Tools/TaskCrudTool.cs           # + priority/dueDate/clearDueDate/overdueOnly parameters
├── Components/Pages/TaskList.razor # + inputs, badges, overdue marker, filters
└── Program.cs                      # GET filters/sort, POST params, PUT request type, TimeProvider

tests/CRUDTasksWithAgent.Tests/
├── TaskServiceTests.cs             # + ordering, filters, overdue, defaults
├── ApiTests.cs                     # + sort/filter, 400s, PUT omit/null semantics
├── TaskCrudToolTests.cs            # + priority/due, invalid input, overdue
├── ScriptedModelTests.cs           # + new intents; each Example routes to its tool
└── Helpers/                        # + fixed-clock TimeProvider
```

**Structure Decision**: Keep the single web project and single test project. New files are small,
single-purpose types; no new projects, repositories or abstraction layers.

## Complexity Tracking

No constitution violations; nothing to justify.

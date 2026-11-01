# Research: Due Dates and Priority for Tasks

All Technical Context items are resolved; there are no open `NEEDS CLARIFICATION` items. Decisions below
follow the user's direction to use the existing stack only (EF Core in-memory via `TasksDbContext`, minimal
API endpoints in `Program.cs`, Blazor Interactive Server).

## D1. Representing priority

- **Decision**: A `TaskPriority` enum (`Low = 0`, `Normal = 1`, `High = 2`) on `TaskItem`, default `Normal`.
  A small custom `JsonConverter` writes the name and reads names case-insensitively, rejecting numbers and
  unknown names.
- **Rationale**: Numeric order gives a cheap descending sort (High first). The default `JsonStringEnumConverter`
  accepts integers, which would let `2` or `7` through; the spec only allows Low/Normal/High in any case
  (clarification Q4). A converter on the enum also makes every serializer (minimal API, agent tool results,
  scripted formatter) emit the same string form.
- **Alternatives considered**: Store a string (loses ordering, invites typos); the framework converter with
  `allowIntegerValues: false` (works for the API only, must be configured per serializer).

## D2. Representing the due date

- **Decision**: `DateOnly? DueDate`. Overdue is derived, never stored: incomplete and `DueDate < today`.
- **Rationale**: Matches "date only, no time zones". `DateOnly` is supported by EF Core, System.Text.Json and
  Blazor `<input type="date">` binding in .NET 10. Deriving overdue avoids stale data.
- **Alternatives considered**: `DateTime`/`DateTimeOffset` (invites time-zone bugs, out of scope); a stored
  `IsOverdue` flag (goes stale at midnight).

## D3. "Today" and testability

- **Decision**: `TaskService` takes an optional `TimeProvider` (default `TimeProvider.System`) and computes
  today as `DateOnly.FromDateTime(clock.GetLocalNow().DateTime)`. `Program.cs` registers
  `TimeProvider.System`; tests substitute a tiny fixed-time `TimeProvider` subclass.
- **Rationale**: "Today" is the server's local date (FR-005). `TimeProvider` is part of .NET, so no package
  is added, and the optional parameter keeps existing `new TaskService(db)` callers compiling.
- **Alternatives considered**: `DateTime.Today` inside the service (not testable); passing `today` into every
  call (noisy, callers could disagree).

## D4. Sort and filter location

- **Decision**: One `TaskService.GetTasksAsync(TaskQuery)` method. Filters (priority, due on or before,
  overdue) are applied in the EF query; ordering is applied in memory. Default order: incomplete first, then
  priority descending, then due date ascending with no date last, then title (case-insensitive ordinal).
  `GetAllTasksAsync()` stays as is (Id order).
- **Rationale**: Sorting a few hundred rows in memory is cheap and keeps title comparison identical across
  providers. Keeping `GetAllTasksAsync()` unchanged protects existing tests and callers.
- **Alternatives considered**: Sorting in the database query (provider-specific string comparison, awkward
  "no date last" ordering); changing `GetAllTasksAsync()` (changes behaviour for every caller).

## D5. Which surfaces use urgency order

- **Decision**: The task list page and the chat agent's list use the urgency order by default. The HTTP API's
  `GET /api/tasks` keeps creation (Id) order unless `sort=priority` is requested.
- **Rationale**: FR-003 and Story 2 require urgency order for the page and chat. For API clients the spec asks
  only that they can request priority sorting (FR-011), so the default stays backward compatible.
- **Alternatives considered**: Changing the API default (could surprise existing clients).

## D6. PUT with omitted fields (full-object PUT problem)

- **Decision**: `PUT /api/tasks/{id}` binds a new `UpdateTaskRequest` instead of `TaskItem`. `Priority` is
  `TaskPriority?`; `DueDate` is a nullable property whose setter records that the property was present in the
  JSON. The handler applies priority only when supplied, and due date only when supplied (a null value clears
  it). `Title` and `IsComplete` keep their current "always copied" behaviour.
- **Rationale**: Satisfies FR-013 (omitting the fields keeps them) and the clarified clear-by-null rule, with
  no new packages. System.Text.Json calls a property setter only when the property appears in the JSON, so a
  presence flag set in the setter distinguishes omitted from null. A client that echoes a full `TaskItem` from
  GET still works, because the new fields are then present.
- **Alternatives considered**: Binding `JsonElement`/`JsonObject` (loses the OpenAPI schema); an `Optional<T>`
  wrapper type with its own converter (more code than a flag); PATCH (changes the public contract).

## D7. Validation and 400 responses

- **Decision**: One shared `TaskInputParser` parses priority (case-insensitive name only) and due date
  (`yyyy-MM-dd`, invariant culture, real calendar dates only). Endpoints return HTTP 400 with a validation
  problem body naming the field; invalid JSON values for the body also yield 400 from the framework. Tools
  return a clear message string and change nothing, matching the tools' existing error style.
- **Rationale**: One place for the rules keeps the page, API, tool and scripted model consistent (FR-010,
  FR-012). The clarification fixed the API status as 400.
- **Alternatives considered**: Separate validation per surface (drift risk).

## D8. Agent tool changes (no new tools)

- **Decision**: Extend existing tools rather than adding new ones: `CreateTaskAsync` gains `priority` and
  `dueDate`; `UpdateTaskAsync` gains `priority`, `dueDate` and `clearDueDate`; `ReadTasksAsync` gains
  `overdueOnly` and returns urgency order. Parameters are strings/optional so a model can omit them.
- **Rationale**: Matches "existing tools plus scripted intents" and avoids touching the tool registration in
  `AgentFrameworkProvider`. `CreateTaskAsync` currently re-reads the "last" task to set completion; it will
  use the task returned by the service instead, because this change touches that code path.
- **Alternatives considered**: Dedicated `SetPriority`/`SetDueDate`/`ListOverdue` tools (more registration and
  test surface for no gain).

## D9. Scripted intents

- **Decision**: Changed row `add` (optional `priority <p>` and `due <date>` suffixes). New rows: `priority`
  (`priority <id> <level>`), `due` (`due <id> <yyyy-mm-dd>`), `clear-due` (`clear due <id>`) and `overdue`
  (`show overdue`). New rows capture ids with `\d+` so ids of 10 or higher work. Each row has a working
  `Example`, so each appears in `Suggestions`. The list formatter shows priority and due date, and the overdue
  row replies "Nothing is overdue." when empty.
- **Rationale**: Constitution principle III and FR-009/FR-010. Existing `complete`, `rename` and `delete` rows
  are not part of this feature; their single-digit id limit stays as a known pre-existing limitation.
- **Alternatives considered**: Natural-language dates ("tomorrow"); out of scope per the spec.

## D10. Page behaviour

- **Decision**: `TaskList.razor` loads via `GetTasksAsync` with component-held filter state, shows a priority
  badge, the due date and an "Overdue" marker, and offers a priority select and date input when adding and
  inline editing for existing tasks, with a clear-date button. Filters: priority, due on or before, overdue
  only; combinable.
- **Rationale**: Meets Stories 1 and 3 within Interactive Server; Blazor binds `DateOnly?` to `type="date"`.
- **Alternatives considered**: A separate edit page (more navigation, more scope).

## D11. Demo data

- **Decision**: Seed priorities and due dates relative to today (for example three days ago, today, next
  week), including at least one overdue incomplete task. Seeding stays Development-only and only when empty.
- **Rationale**: FR-016; relative dates keep the demo meaningful on any day.

## D12. Constraints carried forward

- Priorities and due dates are lost on restart because the in-memory store is unchanged (spec assumption).
- UI behaviour is verified by the manual steps in [quickstart.md](quickstart.md); there is no component
  test framework in the repo and the plan adds none.

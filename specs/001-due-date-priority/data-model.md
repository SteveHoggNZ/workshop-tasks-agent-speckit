# Data Model: Due Dates and Priority for Tasks

## TaskItem (existing entity, extended)

| Field | Type | Rules |
|-------|------|-------|
| `Id` | int | Primary key (unchanged) |
| `Title` | string | Unchanged |
| `IsComplete` | bool | Unchanged |
| `Priority` | `TaskPriority` | New. Default `Normal`. Only `Low`, `Normal`, `High` are valid |
| `DueDate` | `DateOnly?` | New. Optional calendar date, no time of day. `null` means no due date |

No migration is needed: the EF Core in-memory provider builds its model from the type. Existing seed and
test tasks get `Normal` and no due date by default.

## TaskPriority (new enum)

| Name | Value | Meaning |
|------|-------|---------|
| `Low` | 0 | Can wait |
| `Normal` | 1 | Default |
| `High` | 2 | Do first |

JSON form is the name string (`"High"`). Reading accepts any letter case (`"high"`, `"HIGH"`); numbers and
unknown names are rejected (clarification Q4).

## Derived state: Overdue

A task is overdue when `IsComplete == false` and `DueDate` has a value earlier than today. Today is the
server's local date. Overdue is computed on read and is never stored; completed tasks are never overdue.

## Ordering (default urgency order)

1. Incomplete tasks before completed tasks.
2. Within each group: priority descending (High, Normal, Low).
3. Then due date ascending, tasks with no due date last.
4. Then title, case-insensitive ordinal.
5. Then `Id` ascending, so the order is stable for identical titles.

## TaskQuery (new, service-level)

| Field | Type | Meaning |
|-------|------|---------|
| `Priority` | `TaskPriority?` | Only tasks with this priority |
| `DueOnOrBefore` | `DateOnly?` | Only tasks with a due date on or before this date (tasks with no date excluded) |
| `OverdueOnly` | bool | Only incomplete tasks due before today |
| `UrgencyOrder` | bool | When true use the default urgency order; when false use `Id` order |

All set filters combine with AND. A query that matches nothing returns an empty list.

## UpdateTaskRequest (new, PUT body)

| Field | Type | Semantics |
|-------|------|-----------|
| `Title` | string | Copied to the task (unchanged behaviour) |
| `IsComplete` | bool | Copied to the task (unchanged behaviour) |
| `Priority` | `TaskPriority?` | Omitted or null: unchanged. Present: set |
| `DueDate` | `DateOnly?` | Omitted: unchanged. Present and null: cleared. Present with a date: set |

Whether `DueDate` was present is recorded by the property setter (read-only flag, not serialized).

## State transitions

- Create: `Priority = Normal` and `DueDate = null` unless supplied.
- Update priority or due date at any time; clearing the due date is allowed.
- Completing a task removes its overdue state and moves it below all incomplete tasks.
- Un-completing a task restores its position and, if the due date has passed, its overdue state.

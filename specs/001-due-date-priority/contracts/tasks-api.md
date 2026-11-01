# Contract: Tasks HTTP API

Base path `/api/tasks`. JSON bodies use camelCase. Priority is the string `Low`, `Normal` or `High`
(any letter case accepted on input, always returned as shown). Dates are `yyyy-MM-dd`.

## Task representation

```json
{ "id": 7, "title": "Send report", "isComplete": false, "priority": "High", "dueDate": "2026-11-05" }
```

`dueDate` is `null` when there is none. Overdue is derived by clients, not part of the representation.

## GET /api/tasks

| Query parameter | Type | Meaning |
|-----------------|------|---------|
| `priority` | string | Only tasks with this priority |
| `dueBefore` | date | Only tasks with a due date on or before this date |
| `overdue` | bool | `true`: only incomplete tasks with a due date before today |
| `sort` | string | `priority`: urgency order. Omitted: creation (`id`) order |

- Parameters combine with AND. No matches returns `200` with `[]`.
- Invalid `priority`, `dueBefore`, `overdue` or `sort` returns `400` with a problem body naming the parameter.
- Urgency order is defined in [data-model.md](../data-model.md).

## GET /api/tasks/{id}

`200` with the task (now including `priority` and `dueDate`), or `404`.

## POST /api/tasks

`title` stays a query parameter. New optional query parameters `priority` and `dueDate`.

- `201` with `Location` and the created task (`Normal` and null due date when not supplied).
- Invalid `priority` or `dueDate` returns `400`; no task is created.

## PUT /api/tasks/{id}

```json
{ "title": "Send report", "isComplete": false, "priority": "High", "dueDate": "2026-11-05" }
```

| Field | Omitted | `null` | Value |
|-------|---------|--------|-------|
| `title`, `isComplete` | existing behaviour | n/a | set |
| `priority` | unchanged | unchanged | set |
| `dueDate` | unchanged | cleared | set |

- `204` on success, `404` if the task does not exist.
- Invalid `priority` or `dueDate` (unknown name, number, impossible date, wrong format) returns `400`; the
  task is not changed.

## DELETE /api/tasks/{id}

Unchanged.

## Compatibility

- Clients that ignore the new fields keep working; their updates never reset priority or due date.
- A client that echoes the whole task from GET back to PUT keeps the current values.

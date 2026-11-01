# Contract: App-agent tools and scripted intents

Tools live in `Tools/TaskCrudTool.cs` and call `TaskService`. The tool set and its registration in
`Services/AgentFrameworkProvider.cs` do not change; only parameters do. Invalid input returns a clear message
string and changes nothing.

## Tool parameters

| Tool | New or changed parameters |
|------|---------------------------|
| `CreateTaskAsync` | `priority` (optional: Low/Normal/High, any case), `dueDate` (optional, `yyyy-MM-dd`) |
| `UpdateTaskAsync` | `priority` (optional), `dueDate` (optional, `yyyy-MM-dd`), `clearDueDate` (optional bool) |
| `ReadTasksAsync` | `overdueOnly` (optional bool). Result is in urgency order and includes `priority` and `dueDate` |
| `DeleteTaskAsync` | unchanged |

Invalid priority, invalid date, task not found, and `dueDate` combined with `clearDueDate: true` each return a
message naming the problem (exact wording is an implementation detail).

## Scripted intents (`Services/Scripted/ScriptedIntents.cs`)

Rows match in table order; new rows capture ids with `\d+`. Every row has a working `Example`, which is
automatically offered as a suggested prompt.

| Row | Usage | Example | Tool call |
|-----|-------|---------|-----------|
| `add` (changed) | `add task <title> [priority <level>] [due <yyyy-mm-dd>]` | `add task Send report priority high due 2026-12-01` | `CreateTask { title, priority?, dueDate? }` |
| `priority` (new) | `priority <id> <level>` | `priority 3 high` | `UpdateTask { id, priority }` |
| `due` (new) | `due <id> <yyyy-mm-dd>` | `due 3 2026-12-01` | `UpdateTask { id, dueDate }` |
| `clear-due` (new) | `clear due <id>` | `clear due 3` | `UpdateTask { id, clearDueDate: true }` |
| `overdue` (new) | `show overdue` | `show overdue` | `ReadTasks { overdueOnly: true }` |
| `list` (changed reply) | `list tasks` | `list tasks` | `ReadTasks {}` |

- `add task Buy milk` (no suffixes) behaves exactly as today.
- An unrecognised priority or date is passed through and answered with the tool's message; nothing changes.
- `list` and `overdue` replies show `<id>. [ ] <title> (<Priority>, due <date>)`; no due date omits the `due`
  part. The overdue reply is `Nothing is overdue.` when empty.
- Existing `complete`, `rename` and `delete` rows are unchanged.

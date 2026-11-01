# Quickstart: Validating Due Dates and Priority

Run from the repository root. Details: [data-model.md](data-model.md),
[contracts/tasks-api.md](contracts/tasks-api.md), [contracts/agent-tools-and-intents.md](contracts/agent-tools-and-intents.md).

## Prerequisites

- .NET 10 SDK and Node.js (for the lint check). No keys or network are needed (scripted model is the default).

## 1. Automated checks

```bash
dotnet test tests/CRUDTasksWithAgent.Tests
node scripts/lint-count.mjs --check
```

Expected: all tests pass; the warning count stays at or below the baseline of 19 in `docs/lint-baseline.md`.

## 2. Run the app

```bash
dotnet run --project src/CRUDTasksWithAgent
```

Open the local URL shown. Demo data is seeded in Development with mixed priorities and due dates, including
one overdue incomplete task.

## 3. Task list page (Stories 1 and 3)

1. The most urgent incomplete task is first; completed tasks are at the bottom.
2. An overdue incomplete task shows an "Overdue" marker; completed tasks never do.
3. Add a task with no priority or date: it shows `Normal` and no due date.
4. Change a task's priority and due date, then clear its date; the order updates each time.
5. Filter by High, then "due on or before" a date, then overdue only, then combine them. A filter with no
   matches shows an empty list with no error.
6. With 20 or more open tasks, time how long it takes to name the most urgent incomplete task (target: under
   5 seconds).

## 4. Chat agent, offline (Story 2)

Open the chat page. Confirm each new example appears as a suggested prompt, then run:

```text
add task Send report priority high due 2026-12-01
priority 12 low
due 3 2026-12-05
clear due 3
show overdue
list tasks
```

Expected: confirmations, the list shows priority and due date in urgency order, `show overdue` lists only
overdue incomplete tasks (or "Nothing is overdue."). `priority 3 urgent` and `due 3 2026-02-30` reply with a
clear message and change nothing. Repeat the 5-second check from the chat.

## 5. HTTP API (Story 4)

```bash
curl -s "http://localhost:5000/api/tasks?sort=priority"
curl -s "http://localhost:5000/api/tasks?priority=high&dueBefore=2026-12-31"
curl -s "http://localhost:5000/api/tasks?overdue=true"
curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:5000/api/tasks?priority=urgent"
curl -s -X PUT -H "Content-Type: application/json" -d '{"title":"x","isComplete":false}' "http://localhost:5000/api/tasks/1"
curl -s -X PUT -H "Content-Type: application/json" -d '{"title":"x","isComplete":false,"dueDate":null}' "http://localhost:5000/api/tasks/1"
```

Expected: sorted and filtered results; `400` for the invalid priority; the first PUT leaves priority and due
date unchanged; the second clears the due date. (Use the port printed by `dotnet run`.)

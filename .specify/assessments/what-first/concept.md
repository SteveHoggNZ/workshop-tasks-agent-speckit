# Concept: Deciding what to work on first

- **Slug**: what-first
- **Created**: 2026-11-01T19:40:34+13:00
- **Recommended option**: B — Due date plus priority, with sort and filter

## Options

### Option A — Priority only
- **Sketch**: Each task carries a priority (Low, Normal, High; new tasks are Normal). The list shows it
  and sorts High first, and the chat agent can set and show priority. There are no dates.
- **Appetite**: small
- **Trade-offs**: Wins on simplicity and the smallest change surface. Sacrifices real deadlines: it
  answers "urgent" but not "due date", so part of the stated support demand is unmet and B4's upcoming
  section stays blocked. Priority is subjective, so users must maintain it by hand as work changes.
- **Rabbit holes**: Deciding the default ordering (priority-first vs. creation order); how an existing
  client that does not send priority is treated on update.

### Option B — Due date plus priority, with sort and filter
- **Sketch**: Tasks carry an optional due date (date only) and a priority. The list defaults to the most
  urgent first (priority, then earliest due date, no date last). Overdue incomplete tasks are visibly
  marked. Users can filter by priority, due-before date and overdue. The chat agent can set and change
  both fields and shows them in task lists, working offline through scripted commands.
- **Appetite**: small to medium (about a day for an agent with tests, per the stated budget; B1 is
  sized M in the backlog)
- **Trade-offs**: Wins by covering both "urgent" and "due date" requests, matches B1, and unblocks B4 and
  B5. Sacrifices a larger change surface across every layer, a changed default order, and more states
  to explain. Without persistence, values vanish on restart.
- **Rabbit holes**:
  - Update semantics: the whole-object PUT can silently reset new fields for existing clients.
  - Date rules: what "today" and "overdue" mean (server date vs. user time zone).
  - Combining filters and sort in one view and in the chat agent.
  - Making scripted offline commands cover date entry (date parsing is a scope trap).
  - Showing new fields in the scripted `list` reply.

### Option C — Smallest marker: an "urgent" flag (do the minimum)
- **Sketch**: One boolean "urgent" per task, floated to the top and highlighted in the list and chat
  agent. No dates and no levels.
- **Appetite**: small
- **Trade-offs**: Cheapest and easiest to understand. But a binary flag loses ordering once many tasks
  are urgent, and does not address "due date". It risks a second feature soon after.
- **Rabbit holes**: A flag that is never cleared; users marking everything urgent.

### Option D — Do nothing
- **Sketch**: Leave the list as is and let users keep working around it.
- **Appetite**: none
- **Trade-offs**: No cost or risk, but support requests continue and B4 and B5 stay blocked (see
  Cost of Inaction in problem.md).
- **Rabbit holes**: None.

## Recommendation

Proceed with **Option B**, trimmed to the problem's goal. Only B answers both stated asks ("urgent" and
"due date"), and the success metric (find the most urgent incomplete task in under 5 seconds with 20 or
more open tasks) depends on an ordering that reflects both priority and deadline, which A and C cannot
produce. It also fits the small appetite if date entry stays simple and the persistence work (B2) stays
out. If the unverified support requests turn out to ask only for an "urgent" marker, Option A is the
fallback and should be revisited at `/speckit-assess-decide`.

## Out of Scope (for the recommended option)

- Shared or team tasks.
- Notifications and reminders.
- Recurring tasks.
- Persistence across restarts (B2) and the other unrelated backlog items.
- Times of day and time-zone handling (dates only).
- Natural-language date parsing beyond a simple, fixed format.

## Assumptions to Validate

- Users want both a due date and a priority (the support requests are unverified).
- Dates-only with server local date is acceptable for "today" and "overdue".
- Losing priority and due dates on restart is acceptable while the app uses the in-memory database.
- Changing the default list order to most-urgent-first will not harm users who rely on creation order.
- An agent can deliver this, with tests at service, API and tool levels, in about a day.
- Existing clients can keep calling the update endpoint without wiping the new fields.

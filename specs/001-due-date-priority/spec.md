# Feature Specification: Due Dates and Priority for Tasks

**Feature Branch**: `001-due-date-priority`

**Created**: 2026-11-01

**Status**: Draft

**Input**: User description: "Use the handoff summary in .specify/assessments/what-first/decision.md to specify the approved concept."

## Clarifications

### Session 2026-11-01

- Q: Priority levels, default, due-date format, "today", overdue rule, default sort, combinable filters, chat agent scope → A: Confirmed as already specified (Low/Normal/High, default Normal; date only; server-local today; overdue = before today and not complete; priority then due date then title; filters combinable; chat via existing tools plus scripted intents)
- Q: What response does the programmatic task interface give for an invalid priority or date? → A: HTTP 400
- Q: Where should completed tasks appear in the default list order? → A: All incomplete tasks first, then completed tasks; each group uses the same priority, due date, title order
- Q: How should an API client remove a task's due date, given that omitting the field must keep the existing value? → A: Sending the due date explicitly as null clears it; leaving it out keeps it
- Q: Should the chat agent filter the task list? → A: Full list in default order, plus a command to show overdue tasks only; no other chat filters
- Q: Should priority values be case-sensitive? → A: Case-insensitive everywhere; stored and shown as Low, Normal or High

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See the most urgent task first (Priority: P1)

An individual with a long task list wants to open the task list page and immediately see which incomplete task to work on next. Each task can carry a priority (Low, Normal or High) and an optional due date. The list shows the most urgent tasks first, and overdue incomplete tasks are visibly marked.

**Why this priority**: This is the core problem: users cannot tell what to do first. Without urgency data and a list that reflects it, nothing else delivers value.

**Independent Test**: Create tasks with different priorities and due dates on the task list page and confirm the order and the overdue marking. This delivers value on its own, with no filtering and no chat agent.

**Acceptance Scenarios**:

1. **Given** a new task created with no due date and no priority, **When** it is saved, **Then** it has no due date and the priority Normal.
2. **Given** several incomplete tasks with different priorities and due dates, **When** the user opens the task list, **Then** incomplete tasks are listed before completed tasks, and each group is ordered by priority (High first), then by earliest due date (tasks without a due date last), then by title.
3. **Given** an incomplete task whose due date is before today, **When** the task list is shown, **Then** that task is visibly marked as overdue.
4. **Given** a completed task whose due date is before today, **When** the task list is shown, **Then** it is not marked as overdue.
5. **Given** a task, **When** the user changes its priority or due date, or clears the due date, **Then** the list updates to show the new value and order.

---

### User Story 2 - Manage urgency with the chat agent (Priority: P1)

A user who prefers the chat agent wants to set and change a task's priority and due date, and see them when listing tasks, without leaving the chat. This works offline with the scripted model, and the new commands appear among the suggested prompts.

**Why this priority**: The problem statement requires the same answer from both the task list page and the chat agent, and the success metric covers both.

**Independent Test**: In the chat, add a task with a due date and a priority, change them, then list tasks, using the scripted model with no network or keys.

**Acceptance Scenarios**:

1. **Given** the chat agent, **When** the user adds a task with a due date and a priority, **Then** the task is created with both values and the agent confirms.
2. **Given** an existing task, **When** the user asks the agent to change its priority or due date, **Then** only that value changes and the agent confirms.
3. **Given** tasks with priorities and due dates, **When** the user asks the agent to list tasks, **Then** each task shows its priority and due date, with the most urgent incomplete tasks first.
4. **Given** overdue and non-overdue tasks, **When** the user asks the agent to show overdue tasks, **Then** only incomplete tasks with a due date before today are listed, or a clear "nothing overdue" reply if there are none.
5. **Given** the scripted model with no network or keys, **When** the user views the suggested prompts, **Then** each new command appears as a suggestion that works as written.
6. **Given** a task with an id of 10 or higher, **When** the user changes its priority or due date through the chat agent, **Then** the correct task is updated.
7. **Given** a priority or date the agent cannot understand, **When** the user submits it, **Then** the agent replies with a clear message and no task changes.

---

### User Story 3 - Filter to what matters now (Priority: P2)

A user with many open tasks wants to narrow the task list to a single priority, to tasks due on or before a date, or to overdue tasks only, and to combine these filters.

**Why this priority**: Sorting already surfaces urgency. Filters speed up the "what do I do now" decision when the list is long, but the feature is useful without them.

**Independent Test**: Apply each filter alone and in combination on the task list page, and through the programmatic task interface, and confirm only matching tasks are shown.

**Acceptance Scenarios**:

1. **Given** tasks with mixed priorities, **When** the user filters by High, **Then** only High tasks are shown.
2. **Given** tasks with due dates, **When** the user filters by "due on or before" a date, **Then** only tasks due on or before that date are shown, and tasks without a due date are not.
3. **Given** overdue and non-overdue tasks, **When** the user filters to overdue, **Then** only incomplete tasks with a due date before today are shown.
4. **Given** more than one filter selected, **When** the list is shown, **Then** only tasks that match all of them are shown.
5. **Given** filters that match no tasks, **When** the list is shown, **Then** it is empty, with no error.

---

### User Story 4 - Other clients can read and set urgency safely (Priority: P2)

Other programs that use the task service (for example, external agents and scripts) can read, filter, sort, create and update tasks with priority and due date. Existing clients that do not know about the new fields keep working and do not accidentally erase them.

**Why this priority**: Without this, existing clients could silently destroy users' urgency data, which would undermine trust in the feature.

**Independent Test**: Update a task through the programmatic interface with a request that omits priority and due date, then confirm those values are unchanged.

**Acceptance Scenarios**:

1. **Given** the programmatic task interface, **When** a client requests tasks sorted by priority, **Then** High priority tasks come first.
2. **Given** tasks with due dates, **When** a client requests tasks due before a date, **Then** only tasks due on or before that date are returned.
3. **Given** a task with a priority and due date, **When** an existing client updates only the title or completion status, **Then** the priority and due date are unchanged.
4. **Given** a task with a due date, **When** a client updates it with the due date supplied explicitly as null, **Then** the due date is cleared.
5. **Given** an invalid priority value or an invalid date in a request, **When** it is sent, **Then** the request is rejected with an HTTP 400 response and a clear error, and nothing is changed.

---

### Edge Cases

- A due date is today: the task is due but not overdue.
- Several tasks have the same priority and due date: they fall back to title order, so ordering is stable.
- A task without a due date and a task with a far-future due date at the same priority: the dated task is listed first.
- A task is completed: it is no longer overdue, has no overdue marking, and is listed after all incomplete tasks.
- A priority value is not Low, Normal or High, ignoring letter case ("high" and "HIGH" are accepted as High): rejected rather than guessed.
- A due date is invalid or not a real calendar date (for example, 30 February): rejected with a clear message.
- A due date in the past is entered for a new task: accepted, and the task is shown as overdue.
- Existing demo tasks have no priority or due date: they appear as Normal with no due date.
- Data does not survive an application restart in the current setup: priorities and due dates are lost along with the tasks themselves.
- The chat agent receives a request that sets a priority or due date for a task that does not exist: a clear "not found" reply, and nothing is changed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each task MUST be able to carry a priority of Low, Normal or High (accepted in any letter case, always stored and shown as Low, Normal or High), and an optional due date (a calendar date with no time of day).
- **FR-002**: A task created without a priority MUST have the priority Normal, and a task created without a due date MUST have no due date.
- **FR-003**: The task list MUST by default list all incomplete tasks first, then completed tasks; within each group, tasks MUST be ordered by priority (High first), then by due date (earliest first, tasks without a due date last), then by title.
- **FR-004**: An incomplete task whose due date is before today MUST be visibly marked as overdue on the task list page; completed tasks MUST NOT be marked as overdue.
- **FR-005**: "Today" MUST mean the current date on the server's local clock.
- **FR-006**: Users MUST be able to set and change a task's priority and due date, and to clear its due date, from the task list page.
- **FR-007**: Users MUST be able to filter the task list by priority, by tasks due on or before a given date, and by overdue; filters MUST be combinable, and a filter with no matches MUST give an empty list, not an error.
- **FR-008**: The chat agent MUST be able to create tasks with a priority and due date, change either value on an existing task (for any task id), show both when listing tasks, and show only overdue tasks on request. Other filters are not required in the chat.
- **FR-009**: Every new or changed chat agent capability MUST have a matching command in the offline scripted model with a working example, and that example MUST appear among the suggested prompts in the chat.
- **FR-010**: The scripted model MUST accept due dates in a single, simple fixed format (YYYY-MM-DD) and MUST reply with a clear message, without changing any task, when a priority or date is not understood.
- **FR-011**: The programmatic task interface MUST support returning tasks sorted by priority, filtered by priority, filtered by due on or before a date, and filtered by overdue, and MUST include each task's priority and due date.
- **FR-012**: The programmatic task interface MUST reject an invalid priority or invalid due date with an HTTP 400 response and a clear error message, and MUST NOT change any task when it does.
- **FR-013**: Updates from existing clients that do not supply priority or due date MUST NOT change those values on the task. A client MUST be able to clear a due date by supplying the due date explicitly as null.
- **FR-014**: The feature MUST keep working offline with no keys or external services, and MUST not change the existing behaviour for creating, completing, renaming and deleting tasks.
- **FR-015**: Every behaviour change MUST be covered by automated tests at the service, programmatic interface and chat agent tool levels, as relevant.
- **FR-016**: The sample (demo) tasks SHOULD include a mix of priorities and due dates, including at least one overdue task, so the feature can be demonstrated.

### Key Entities

- **Task**: A personal to-do item with a title, completion status, a priority (Low, Normal or High; default Normal) and an optional due date (date only).
- **Priority**: One of three ordered levels, Low, Normal, High, used to rank tasks (High first).
- **Overdue state**: A derived state, not stored. A task is overdue when it is incomplete and its due date is before today.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With 20 or more open tasks, a user can identify their most urgent incomplete task in under 5 seconds on the task list page.
- **SC-002**: With 20 or more open tasks, a user can identify their most urgent incomplete task in under 5 seconds through the chat agent.
- **SC-003**: 100% of tasks with a due date before today that are incomplete are marked overdue, and 0% of completed tasks are.
- **SC-004**: Every new chat command works offline with no network access or keys, and each appears as a suggested prompt in the chat.
- **SC-005**: Updating a task from an existing client that omits the new fields leaves priority and due date unchanged in 100% of tested cases.
- **SC-006**: Support requests asking for "urgent" or "due date" markers fall to zero in the month after release (baseline: three in the last month, not yet verified).

## Assumptions

- Users are individuals managing their own tasks; shared or team tasks are out of scope.
- Priority has exactly three levels (Low, Normal, High), with Normal as the default.
- Due dates are calendar dates only (no time of day and no time zones); "today" is the server's local date.
- Most-urgent-first becomes the default order of the task list, replacing creation order.
- Priorities and due dates are held in the same temporary store as tasks, so they are lost on restart; keeping data across restarts is a separate feature.
- The 5-second success metrics will be checked by a simple timed walkthrough against a seeded list of 20 or more tasks, as no usage instrumentation exists.
- The three support requests have not been reviewed; if they ask only for an "urgent" marker, the scope could narrow to priority alone.
- The existing limit in the scripted model, where only single-digit task ids are recognised by some commands, is addressed for the commands added or changed by this feature.
- Out of scope: notifications and reminders, recurring tasks, times of day and time zones, natural-language date entry, persistence across restarts, and other backlog items.

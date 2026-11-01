# Problem Definition: Deciding what to work on first

- **Slug**: what-first
- **Created**: 2026-11-01T19:39:34+13:00
- **Inputs used**: intake.md, research.md, user input

## Problem Statement

Individuals managing their own tasks in this app cannot tell which incomplete task to do next. The
task list is a single flat list in creation order, and nothing in it, on the task list page or in the
chat agent, signals urgency or deadlines. This matters now because users are asking for it (three
support requests this month, unverified) and it is a recorded backlog need (B1).

## Affected Users & Stakeholders

- **Users**: Individuals managing their own personal task list, through the task list page and the chat
  agent — they must scan the whole list and rely on creation order or their own memory to decide what
  to do next (source: intake.md; research.md, Users & Demand).
- **Stakeholders**: Support, which receives the requests for urgency/due-date markers, and the app
  maintainers who own the backlog — [NEEDS CLARIFICATION: confirm who owns the decision and who
  handles the support requests].

## Goals

- A user can quickly identify which incomplete task is most urgent, from both the task list page and
  the chat agent.
- Urgency information is available in both surfaces, so neither is a second-class route.
- Existing task management (create, complete, rename, delete) keeps working for users who ignore
  urgency.

## Non-Goals

- Shared or team tasks, and multi-user coordination (individuals only).
- Notifications and reminders.
- Recurring tasks.
- Persistence across restarts (backlog B2) and other unrelated backlog items.

## Success Metrics

- With 20 or more open tasks, a user can identify their most urgent incomplete task in under 5 seconds,
  from the task list page and from the chat agent. Quantitative (baseline: unknown, not measured;
  expected to be poor because order is creation-only).
- Support requests asking for "urgent" or "due date" markers fall to zero in the month after release.
  Quantitative (baseline: 3 this month, unverified).
- [NEEDS CLARIFICATION: How will the 5-second target be measured? No usage instrumentation or user
  testing exists in the repo.]

## Cost of Inaction

Users keep working around the gap, for example by editing titles or relying on creation order. Support
requests continue, and backlog items that depend on urgency data (B4 weekly summary, B5 triage) stay
blocked (source: research.md, Market & Context).

## Open Questions

- [NEEDS CLARIFICATION: Can the three support requests be reviewed? Do users want an urgency marker, a
  deadline, or both?]
- [NEEDS CLARIFICATION: What is a typical number of open tasks per user? The metric assumes 20+; no data
  confirms it.]
- [NEEDS CLARIFICATION: Is losing urgency information on restart acceptable, given the in-memory
  database?]
- [NEEDS CLARIFICATION: What rule defines "today" and "overdue" for users (server date vs. user
  time zone)?]
- [NEEDS CLARIFICATION: Should creation order stay the default view, or should urgency change the
  default ordering?]

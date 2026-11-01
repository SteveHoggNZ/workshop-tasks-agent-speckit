# Decision: Deciding what to work on first

- **Slug**: what-first
- **Decided**: 2026-11-01T19:43:43+13:00
- **Verdict**: go
- **Artifacts reviewed**: intake.md, research.md, problem.md, concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | The gap is verified in code: the list is ordered by `Id` only and nothing marks urgency or deadlines (research.md). B1 independently records the same need (docs/backlog.md). |
| Evidence strength | adequate | The problem and its constraints are well-evidenced (high confidence). Demand is not: the three support requests are stated, not visible in the repo (research.md, low confidence). B1 corroborates the want, but it is a planned item, not observed behaviour. Acknowledged: the demand evidence alone would be weak. |
| Value vs. inaction | adequate | Inaction leaves support requests open and blocks B4 and B5, which depend on B1 (problem.md; research.md). Value of the exact scope is plausible but unmeasured. |
| Feasibility / appetite | strong | Option B fits the stated one-day appetite for an agent. All layers and files are identified, and no new packages or infrastructure are needed (concept.md; research.md). |
| Strategic fit | strong | Fits the constitution: layered change, tests at each level, offline scripted intents, no new packages, and it stays on Blazor Interactive Server (.specify/memory/constitution.md). |
| Risk posture | adequate | Key risks are identified: full-object PUT resetting fields, date rules, data loss on restart, and offline date entry. Each has a named mitigation path in specification, but none is resolved yet (concept.md rabbit holes). |

## Verdict & Rationale

**Go.** The problem is real and verified in the code, a recommended option (B) exists within a small
appetite, and the idea fits the constitution. Evidence strength is `adequate` rather than `strong` only
because the demand signal is unverified. The cost of being wrong is small: a day of work, and Option A
(priority only) is a ready fallback if the support requests turn out to ask only for an "urgent" marker.
The unverified items are carried forward as open questions for specification rather than blocking.

## If needs-clarification

Not applicable.

## If go — Handoff to `/speckit-specify`

- **Problem**: Individuals managing their own tasks cannot tell which incomplete task to do next, because
  the task list is a flat, creation-ordered list with no urgency or deadline signal on the task list
  page or in the chat agent.
- **Chosen approach**: Option B: an optional due date (date only) and a priority (Low, Normal, High; new
  tasks default to Normal). The list defaults to most urgent first (priority, then earliest due date,
  no date last). Overdue incomplete tasks are marked. Filters cover priority, due-before date and
  overdue. The chat agent can set, change and show both fields, with matching offline scripted intents.
- **In scope**: `DueDate` and `Priority` on tasks; sorting and filtering in the service, API and task
  list page; app-agent tool parameters and scripted intents with working examples; tests at service,
  API and tool levels; demo-data updates so the feature is demonstrable.
- **Out of scope**: Shared or team tasks; notifications and reminders; recurring tasks; persistence
  across restarts (B2); times of day and time zones; natural-language date parsing beyond a simple fixed
  format; other backlog items.
- **Success metrics**: With 20 or more open tasks, a user can identify their most urgent incomplete task
  in under 5 seconds from both the task list and the chat agent (baseline unknown). Support requests for
  "urgent" or "due date" markers drop to zero the month after release (baseline: 3, unverified).
- **Carried-forward open questions**:
  - [NEEDS CLARIFICATION: Can the three support requests be reviewed? Do users want an urgency marker, a
    deadline, or both? If only an urgency marker, fall back to Option A.]
  - [NEEDS CLARIFICATION: Should PUT stay a full-object replacement, and how are existing clients that
    omit the new fields protected from resetting them?]
  - [NEEDS CLARIFICATION: Which rule defines "today" and "overdue" (server local date vs. user time
    zone)?]
  - [NEEDS CLARIFICATION: Should creation order remain the default, or should most-urgent-first become
    the default ordering?]
  - [NEEDS CLARIFICATION: Is losing priority and due dates on restart acceptable with the in-memory
    database?]
  - [NEEDS CLARIFICATION: How will the 5-second target be measured?]
  - [NEEDS CLARIFICATION: The scripted `complete`, `delete` and `rename` patterns match a single digit
    id; should this be addressed as part of the intent changes?]

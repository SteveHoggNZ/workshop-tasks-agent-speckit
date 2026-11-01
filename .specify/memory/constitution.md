<!--
Sync Impact Report
- Version change: unratified scaffold → 1.0.0
- Modified principles: template placeholders → seven project-specific principles
- Added sections: Technology and Runtime Constraints; Development Workflow and Quality Gates
- Removed sections: none
- Follow-up TODOs: Confirm the original ratification date.
-->

# CRUD Tasks with Agent Constitution

## Core Principles

### I. Behaviour Changes Require Layered Tests
Every behaviour change MUST ship with xUnit tests in
`tests/CRUDTasksWithAgent.Tests`, covering the service, minimal API, and app-agent
tool levels as relevant. Tests MUST verify the public contract and the changed
behaviour rather than implementation details. This keeps changes safe across the
application's layered entry points.

### II. Preserve the Application Layering
The application MUST preserve the dependency flow
`Model -> TaskService -> minimal API endpoints in Program.cs -> Blazor page`.
App-agent tools in `Tools/TaskCrudTool.cs` MUST call `TaskService` and MUST NOT
access the `DbContext` directly. This separation keeps domain operations
centralized and prevents UI, API, and agent paths from diverging.

### III. App-Agent Tools Must Be Offline-Discoverable
Every new or changed app-agent tool MUST have a matching scripted intent in
`Services/Scripted/ScriptedIntents.cs`, including a working `Example`. The
scripted intent MUST make the tool usable offline and cause it to appear as a
suggested prompt in chat. This ensures tool changes remain demonstrable without
external model access.

### IV. Warnings Must Not Increase
Changes MUST NOT add build warnings beyond the baseline recorded in
`docs/lint-baseline.md`. Contributors MUST verify this with
`node scripts/lint-count.mjs --check`. Warning-count changes require either
remediation or an explicit, reviewed baseline update tied to the change.

### V. Dependencies Require Written Justification
No new NuGet package MAY be added without a written justification in the
implementation plan. The justification MUST explain why existing framework or
repository capabilities are insufficient and identify the package's scope and
maintenance impact. This limits unnecessary dependency and supply-chain risk.

### VI. Offline Operation Is a Required Runtime Mode
The app MUST remain runnable offline with no keys or external model credentials.
The scripted model MUST remain the default. Features that require an online
service MUST provide an explicit, tested integration path without making that
service a startup or core development requirement.

### VII. Keep Blazor Interactive Server
The UI MUST remain Blazor Interactive Server. Moving UI execution to WebAssembly
is out of scope unless an approved feature specification explicitly changes this
constraint. This preserves the current hosting model and its server-side
application integration.

## Technology and Runtime Constraints

The project is a .NET 10 Blazor Interactive Server task application. Changes
MUST use the repository's existing framework and project structure unless a
feature specification authorizes a change. The default development and
demonstration path MUST work without network access, secrets, or model keys.

## Development Workflow and Quality Gates

Implementation plans MUST identify affected layers, required tests, offline
scripted intents, and any proposed NuGet additions with their justifications.
Before a change is considered complete, the relevant xUnit tests MUST pass and
`node scripts/lint-count.mjs --check` MUST pass. Reviews MUST check layering,
offline operation, warning-count compliance, and scope adherence.

## Governance

This constitution supersedes conflicting project practices for the repository.
Amendments MUST be made through the Spec Kit constitution workflow, document
the affected principles and rationale in the Sync Impact Report, and update the
version and last-amended date. Amendments MUST preserve existing requirements
unless they explicitly redefine or remove them.

The constitution follows semantic versioning:

- MAJOR increments for backward-incompatible removals or redefinitions of
  principles.
- MINOR increments for new principles or materially expanded governance.
- PATCH increments for clarifications, wording changes, and other
  non-semantic refinements.

Every implementation plan and review MUST assess compliance with this
constitution. Quality gates MUST be checked before completion, and any
exception MUST be documented in the relevant plan or review with its scope,
rationale, and approval. The constitution MUST be reviewed whenever the
application architecture, runtime mode, testing strategy, or agent-tool
workflow materially changes.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): confirm original
adoption date | **Last Amended**: 2026-11-01

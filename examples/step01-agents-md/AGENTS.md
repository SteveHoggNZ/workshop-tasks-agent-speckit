# AGENTS.md

## Stack

- .NET 10, ASP.NET Core minimal API + Blazor (InteractiveServer), EF Core (in-memory provider).
- Microsoft Agent Framework + Microsoft.Extensions.AI for the in-app agent.
- xUnit tests.

## Layout

- `src/CRUDTasksWithAgent/Components/Pages/` — Blazor pages.
- `src/CRUDTasksWithAgent/Services/` — `TaskService` (data access), agent providers, lab services.
- `src/CRUDTasksWithAgent/Services/Scripted/` — offline scripted model; `ScriptedIntents.cs` is its command table.
- `src/CRUDTasksWithAgent/Tools/TaskCrudTool.cs` — functions the app agent can call.
- `src/CRUDTasksWithAgent/Models/` — `TaskItem`, `TasksDbContext`.
- `tests/CRUDTasksWithAgent.Tests/` — unit and API tests; helpers in `Helpers/`.
- `infra/` — Bicep for Azure.

## Rules

- `TaskService` is the only data access. Components, endpoints and tools never inject `TasksDbContext`.
- Get the current time from an injected `TimeProvider`. Never call `DateTime.Now`, `DateTime.UtcNow` or `DateTimeOffset.Now` directly.
- UTC for storage; local date for calendar boundaries such as week start.
- Put new code in new files where possible. Keep changes small.
- Do not edit `infra/` without asking first.

## Definition of done

- New behaviour has a test in `tests/CRUDTasksWithAgent.Tests/`.
- New pages are listed in `docs/architecture.md`.
- `node scripts/lint-count.mjs` was run and its total reported.

## Commands

```sh
dotnet build
dotnet test
node scripts/lint-count.mjs
dotnet run --project src/CRUDTasksWithAgent   # http://localhost:5280
```

Run `dotnet build` and `dotnet test` before you finish. Both must pass.

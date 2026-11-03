---
name: add-agent-tool
description: Add a new tool (function) the in-app task agent can call, including its scripted-model command and tests. Use when asked to add an app-agent tool, a new agent command, or something users should be able to type in /agentfx.
---

# Add an app-agent tool

A tool is only complete when all four steps are done. Do them in order.

## 1. Tool method

File: `src/CRUDTasksWithAgent/Tools/TaskCrudTool.cs`

- Add `public async Task<string> <Name>Async(...)` next to the existing methods.
- Add `[Description("...")]` to the method and to each parameter.
- Use `_taskService` only (never `TasksDbContext`). Return a short human-readable result.

## 2. Register with the agent

File: `src/CRUDTasksWithAgent/Services/AgentFrameworkProvider.cs`

- Add `AIFunctionFactory.Create(taskCrudTool.<Name>Async)` to the `tools:` list.
- The exposed tool name drops the `Async` suffix: `<Name>Async` → `<Name>`.

## 3. Scripted intent

File: `src/CRUDTasksWithAgent/Services/Scripted/ScriptedIntents.cs`

- Add a `new(...)` row to `All` with `Name`, a case-insensitive `Pattern` (use `Options`), `ToolName: "<Name>"`, `Arguments`, `Usage` and `Example`.
- `help` text and suggestions update automatically from `All`.

## 4. Tests

- `tests/CRUDTasksWithAgent.Tests/TaskCrudToolTests.cs` pattern: test the tool against `TestDb.Create()` (`Helpers/TestDb.cs`).
- `tests/CRUDTasksWithAgent.Tests/ScriptedModelTests.cs` pattern: `new ScriptedChatClient()`, send the command, assert the `FunctionCallContent.Name` is `<Name>` and the arguments.
- Put new tests in a new file, e.g. `tests/CRUDTasksWithAgent.Tests/<Name>ToolTests.cs`.

## Check

1. `dotnet build` and `dotnet test` pass, with the new tests listed.
2. Run the app, open http://localhost:5280/agentfx, type `help`: the new command is listed.
3. Type the command: the tool runs and the task list changes.
4. `git status` shows all four files above changed or added.

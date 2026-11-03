---
description: Add a new tool the in-app agent can call
agent: agent
argument-hint: toolName=... description=... command=...
---

Add an app-agent tool named `${input:toolName:ClearCompletedTasks}`.

- What it does: ${input:description:Deletes all completed tasks}
- What users type in the scripted model: `${input:command:clear completed}`

Make all four changes:

1. Add an `async` method `${input:toolName}Async` with `[Description]` attributes to [TaskCrudTool.cs](../../src/CRUDTasksWithAgent/Tools/TaskCrudTool.cs). Use `TaskService` only.
2. Register it with `AIFunctionFactory.Create(...)` in [AgentFrameworkProvider.cs](../../src/CRUDTasksWithAgent/Services/AgentFrameworkProvider.cs).
3. Add a row to `All` in [ScriptedIntents.cs](../../src/CRUDTasksWithAgent/Services/Scripted/ScriptedIntents.cs) with `ToolName: "${input:toolName}"`.
4. Add tests in `tests/CRUDTasksWithAgent.Tests/` for the tool and the scripted command.

Then run `dotnet build` and `dotnet test`.

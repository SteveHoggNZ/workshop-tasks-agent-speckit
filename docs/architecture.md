# Architecture

A small Blazor Server app with a REST API, an in-app chat agent, and an optional Foundry chat page. Everything runs in one ASP.NET Core process on .NET 10.

## Components

| Component | Where | Role |
|---|---|---|
| Task List UI | `Components/Pages/TaskList.razor` (`/`) | CRUD UI for tasks |
| App agent chat UI | `Components/Pages/AgentFrameworkAgent.razor` (`/agentfx`) | Chat with the app agent |
| Foundry chat UI | `Components/Pages/FoundryAgent.razor` (`/foundryagent`) | Stretch module, behind `Features:Foundry` |
| REST API | `Program.cs` (`/api/tasks`) | Minimal API endpoints over `TaskService` |
| OpenAPI | `Program.cs` (`/openapi/v1.json`) | Machine-readable API description for external agents |
| `TaskService` | `Services/TaskService.cs` | The only place that reads and writes tasks |
| `TasksDbContext` | `Models/TasksDbContext.cs` | EF Core context with an in-memory database |
| `AgentFrameworkProvider` | `Services/AgentFrameworkProvider.cs` | Builds the app agent and its conversation thread |
| `ChatClientFactory` | `Services/ChatClientFactory.cs` | Creates the model client for the configured `Llm:Provider` |
| Scripted model | `Services/Scripted/` | Deterministic offline model, the default provider |
| `TaskCrudTool` | `Tools/TaskCrudTool.cs` | Task operations exposed to the app agent as tools |
| Demo data | `Services/DemoData.cs` | Seeds sample tasks, Development only |

## Request flow: UI to database

```
Browser ── SignalR ──> TaskList.razor ──> TaskService ──> TasksDbContext ──> in-memory store
```

The Task List page is an interactive server component. It injects `TaskService` and calls it directly; there is no HTTP hop between the page and the service. `TaskService` and `TaskCrudTool` are scoped services, and `TasksDbContext` is registered with `UseInMemoryDatabase("TasksDb")`, so data lives for the life of the process.

## REST API

The minimal API in `Program.cs` maps these endpoints, all backed by `TaskService`:

| Method | Route | Notes |
|---|---|---|
| GET | `/api/tasks` | List all tasks |
| GET | `/api/tasks/{id}` | Get one task, 404 if missing |
| POST | `/api/tasks?title=...` | Create a task; the title comes from the query string; returns 201 |
| PUT | `/api/tasks/{id}` | Update title and completion from a JSON body; 204 or 404 |
| DELETE | `/api/tasks/{id}` | Delete a task; 204 or 404 |

`app.MapOpenApi()` serves the OpenAPI document at `/openapi/v1.json`. External agents (for example a Foundry OpenAPI tool) can use it to call the API.

## App agent flow

```
AgentFrameworkAgent.razor
  └─> IAgentFrameworkProvider (AgentFrameworkProvider)
        ├─ IChatClientFactory.Create()  ──> ScriptedChatClient | GitHub Models | Ollama | OpenAI | Azure OpenAI
        └─ AIAgent + AgentThread, tools from TaskCrudTool
              └─> TaskCrudTool ──> TaskService ──> TasksDbContext
```

1. The page calls the agent's `RunAsync` with the user's message and the provider's conversation thread.
2. `AgentFrameworkProvider` creates the agent lazily on first access. It asks `IChatClientFactory` for an `IChatClient`, then wraps it as an agent with instructions and four tools: create, read, update and delete task.
3. `ChatClientFactory` reads `Llm:Provider`. The default, `Scripted`, returns the `ScriptedChatClient`; other values build an OpenAI-compatible or Azure OpenAI client. See [model-providers.md](model-providers.md).
4. The model replies either with text or with a function call. The agent framework invokes the matching `TaskCrudTool` method, which calls `TaskService`, and feeds the result back to the model for the final reply.
5. If agent creation fails, the provider logs the error and returns no agent; the chat page shows a "not configured" message and the rest of the app keeps working.

### The scripted model

`ScriptedChatClient` is an `IChatClient` that needs no network. It matches the user's message against the rows in `Services/Scripted/ScriptedIntents.cs`. A match becomes a real function call to a tool; no match returns the help text. To teach it a new command, add a row to that table. Each row (`ScriptedIntent`) has a name, a regex, the tool to call, a way to build the arguments, a usage line for the help text, and an optional formatter for the tool result.

## Foundry stretch module

`FoundryAgent.razor` and `Services/FoundryAgentProvider.cs` chat with an agent that already exists in Microsoft Foundry, using the Foundry project SDK and `DefaultAzureCredential`. It is enabled by `Features:Foundry` plus `FoundryProjectEndpoint` and `FoundryAgentName`. Nothing else in the app depends on it. For deployment, see [deploy-to-azure.md](deploy-to-azure.md).

## Tests

`tests/CRUDTasksWithAgent.Tests` covers `TaskService`, the REST API (through `WebApplicationFactory`), `TaskCrudTool`, and the scripted model end to end. Run it with `dotnet test`.

## Project structure

- `src/CRUDTasksWithAgent/Components/Layout/NavMenu.razor` — Sidebar navigation menu.
- `src/CRUDTasksWithAgent/Components/Layout/MainLayout.razor` — Main layout with sidebar and content area.
- `src/CRUDTasksWithAgent/Components/Pages/TaskList.razor` — Task list CRUD UI.
- `src/CRUDTasksWithAgent/Components/Pages/AgentFrameworkAgent.razor` — Microsoft Agent Framework chat agent UI.
- `src/CRUDTasksWithAgent/Components/Pages/FoundryAgent.razor` — Foundry Agent Service chat UI (stretch module).
- `src/CRUDTasksWithAgent/Models/` — Data models for tasks and chat messages.
- `src/CRUDTasksWithAgent/Services/` — Task service, agent providers, chat client factory, demo data, and the scripted model.
- `src/CRUDTasksWithAgent/Plugins/` — Agent plugin for task CRUD operations.
- `src/CRUDTasksWithAgent/Program.cs` — Service registration, REST endpoints, and OpenAPI.
- `tests/CRUDTasksWithAgent.Tests/` — xUnit tests.
- `infra/` — Bicep and parameter files for Azure deployment.
- `docs/` — Attendee documentation.

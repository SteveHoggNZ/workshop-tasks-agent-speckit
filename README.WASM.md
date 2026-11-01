# Moving the task list to Blazor WebAssembly

This app currently uses **Blazor Interactive Server** (see [README.md](README.md)). This note explains what it would take to run the task list UI **in the browser with WebAssembly (WASM)**, as many production Blazor front ends do, while keeping the API, data and app agent on the server.

> Status: **not done.** This is a plan. The Spec Kit demo uses the app as it is.

## Target shape

| Part | Today | After the upgrade |
| --- | --- | --- |
| Projects | `CRUDTasksWithAgent` (server, everything) | `CRUDTasksWithAgent` (server host and API) + `CRUDTasksWithAgent.Client` (WASM components) |
| Task list page | Interactive Server; calls `TaskService` directly | Interactive WebAssembly; calls `/api/tasks` with `HttpClient` |
| Agent chat pages (`/agentfx`, Foundry) | Interactive Server | **Stay Interactive Server.** They use server-side providers, model keys and EF Core |
| Data | EF Core in-memory, on the server | Unchanged, server only |
| REST API | Minimal API in `Program.cs` | Unchanged. It becomes the UI's only route to the data |

This uses a mixed set of render modes: each page picks its own. That's the normal pattern for a Blazor Web App in .NET 8 and later, so not everything has to move.

## Steps

1. **Create the client project.**

   ```bash
   dotnet new blazorwasm --empty -o src/CRUDTasksWithAgent.Client
   ```

   Then turn it into a Blazor Web App client project:
   - Keep `Sdk="Microsoft.NET.Sdk.BlazorWebAssembly"`.
   - Remove its `wwwroot/index.html` and `App.razor`; the server host renders the page.
   - Keep `Microsoft.AspNetCore.Components.WebAssembly`.
   - Add the project to `CRUDTasksWithAgent.slnx`.

   It's worth generating a reference copy with `dotnet new blazor --interactivity WebAssembly -o /tmp/ref` and diffing against it. The template shows the exact server/client wiring for the installed SDK version.

2. **Wire the server host.**
   - Add the `Microsoft.AspNetCore.Components.WebAssembly.Server` package and a project reference to the client.
   - In `Program.cs`:

     ```csharp
     builder.Services.AddRazorComponents()
         .AddInteractiveServerComponents()
         .AddInteractiveWebAssemblyComponents();

     app.MapRazorComponents<App>()
         .AddInteractiveServerRenderMode()
         .AddInteractiveWebAssemblyRenderMode()
         .AddAdditionalAssemblies(typeof(CRUDTasksWithAgent.Client._Imports).Assembly);
     ```

   - In `Development`, add `app.UseWebAssemblyDebugging()` so the WASM code can be debugged.

3. **Share the contract, not the entity.** Put a `TaskDto` (`Id`, `Title`, `IsComplete`, plus any new fields) in the client project, which the server already references. Keep the EF `TaskItem` entity on the server and map between the two in the API. That way the browser never depends on persistence details.

4. **Add a typed API client in the client project.** Define `ITaskApi` with `GetAllAsync`, `AddAsync`, `UpdateAsync` and `DeleteAsync`, implemented with `HttpClient`. In the client's `Program.cs`:

   ```csharp
   builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
   builder.Services.AddScoped<ITaskApi, HttpTaskApi>();
   ```

   Watch for two things in the current API:
   - `POST /api/tasks` binds `title` from the **query string**.
   - `PUT /api/tasks/{id}` replaces the whole item. A body with only `isComplete` wipes the title; this is deliberate bug **D17**. Always send the full DTO, or fix D17 first.

5. **Move the task list.**
   - Move `Components/Pages/TaskList.razor` into the client project.
   - Change `@rendermode InteractiveServer` to `@rendermode InteractiveWebAssembly`.
   - Replace `@inject TaskService` with `@inject ITaskApi`.
   - Move any layout or shared components it needs, or keep them in the server project if they render statically.

6. **Handle prerendering.** By default the page renders once on the server, then again in WASM. The server pass needs an `ITaskApi` too. Pick one:
   - **Server implementation:** register a server-side `ITaskApi` that calls `TaskService` directly. You get a fast first paint, and it's the template's recommended pattern.
   - **No prerendering:** use `@rendermode @(new InteractiveWebAssemblyRenderMode(prerender: false))`. It's simpler, but shows a loading state.

7. **Leave the agent pages alone.** `AgentFrameworkAgent.razor` and `FoundryAgent.razor` stay `InteractiveServer`. Moving them would need a chat API endpoint, and model keys must never go to the browser.

8. **Update tests and checks.**
   - `ApiTests` should pass unchanged. The API becomes more important, so add tests for anything the client relies on, such as the query-bound `title` and full-object `PUT`.
   - Optionally add bUnit tests for the client component against a fake `ITaskApi`.
   - Re-run the lint baseline: `node scripts/lint-count.mjs --check`. A new project may add warnings (e.g. IDE0161 for file-scoped namespaces), so update `docs/lint-baseline.md` on purpose rather than by accident.
   - Update `README.md`, `docs/architecture.md` and the `scripts/doctor.mjs` expectations if they mention the hosting model.

## How to confirm it worked

- In browser dev tools → Network, loading the task list fetches `.wasm` files from `_framework/`, and task actions show up as `fetch` calls to `/api/tasks`.
- The task list keeps working when you watch the `_blazor` WebSocket. Only the agent pages should use it.
- `dotnet test` passes, and the lint count matches the (updated) baseline.

## Effort and risk

- **Effort:** about half a day to a day for this app, mostly steps 3–6 and the tests.
- **Main risks:**
  - Prerendering surprises: services missing on the server pass, or flicker.
  - DTO drift between client and server.
  - D17 surfacing as soon as the UI goes through `PUT`.
- **A good way to do it:** as a Spec Kit feature. Run `/speckit-specify` on "Run the task list UI in the browser with WebAssembly, keeping agent pages on the server", then let `clarify` raise the prerendering and D17 questions before any code changes.

# AI Development Primitives Lab

A hands-on training lab for learning the building blocks of AI-assisted development — **instructions, prompts, MCP, agents, skills, hooks, plugins**, and spec-driven development with OpenSpec — using a small .NET app as the playground. The labs work in any coding-agent harness; nothing here assumes one vendor.

The playground app is a Blazor task list with a built-in chat agent. It is forked from the Azure sample [app-service-agentic-semantic-kernel-ai-foundry-agent](https://github.com/Azure-Samples/app-service-agentic-semantic-kernel-ai-foundry-agent), which accompanies the tutorial [Build an agentic web app in Azure App Service with Microsoft Agent Framework or Foundry Agent Service (.NET)](https://learn.microsoft.com/azure/app-service/tutorial-ai-agent-web-app-semantic-kernel-foundry-dotnet). Credit to the original authors.

> **Hosting model: Blazor Interactive Server, not WebAssembly.** Components run on the server, and the browser gets UI updates over a SignalR connection (`blazor.web.js`). No .NET code runs in the browser. There is a single web project and no `.Client` WebAssembly project; render modes are set with `@rendermode InteractiveServer`. The components, services and API work the same way in a WebAssembly app. What differs is where the components run, and that a WebAssembly front end calls the REST API over HTTP instead of calling `TaskService` directly. See [README.WASM.md](README.WASM.md) for what moving the task list to WebAssembly would involve.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json`)
- [Node.js](https://nodejs.org/) 22.20 or later
- [Git](https://git-scm.com/)
- Chrome or Chromium
- **A coding-agent harness of your choice** (see [Choose your harness](#choose-your-harness))

Check everything at once:

```bash
node scripts/doctor.mjs
```

> `scripts/doctor.mjs` is being added alongside the labs.

## Run and test

```bash
dotnet run --project src/CRUDTasksWithAgent   # http://localhost:5280
dotnet test                                   # runs tests/CRUDTasksWithAgent.Tests
```

| URL | What |
|---|---|
| http://localhost:5280/ | Task list (demo tasks are seeded in Development) |
| http://localhost:5280/agentfx | Chat with the app agent |
| http://localhost:5280/api/tasks | REST API |
| http://localhost:5280/openapi/v1.json | OpenAPI document |

Data is held in memory, so it resets every time the app restarts.

## Two kinds of agent

This lab involves two different agents. Keep them apart:

| | **App agent** | **Coding agent** |
|---|---|---|
| Where it lives | Inside the product, at `/agentfx` | In your harness (terminal, editor, IDE) |
| What it does | Manages tasks for the app's users through the task tools | Reads, edits and tests this repository for you |
| Built with | Microsoft Agent Framework (C#) | Claude Code, GitHub Copilot, Cursor, etc. |
| Model | Chosen by `Llm:Provider` (scripted by default) | Chosen in your harness |

The labs ask you to use the coding agent to change the app agent. When a lab says "agent" without a qualifier, it names which one.

## App-agent model providers

By default the app agent runs on a deterministic, offline **scripted model** — no keys, no network, same answer every time. You can switch to GitHub Models, Ollama, OpenAI or Azure OpenAI with configuration only. See [docs/model-providers.md](docs/model-providers.md) for settings and the scripted command list.

## Choose your harness

| Tier | Harnesses |
|---|---|
| Tier 1 (fully supported) | Claude Code, GitHub Copilot (VS Code and CLI), Cursor |
| Best effort | Codex and other agents that support project instruction files or MCP |

Labs give a per-harness tab for each tier-1 harness wherever the file names or menus differ. Pick one and stay with it; you can compare with others afterwards.

## Labs

Lab guides live in the `labs/` folder, which is being added. Start there once it lands. Each lab can be validated with the harness-free checker `node scripts/check-lab.mjs NN` (where `NN` is the lab number), also being added.

For background reading:

- [docs/architecture.md](docs/architecture.md) — how the app fits together
- [docs/backlog.md](docs/backlog.md) — small features used for spec-driven labs
- [docs/model-providers.md](docs/model-providers.md) — app-agent models and scripted commands

## Troubleshooting

- **Port in use.** The app listens on 5280 (HTTP) by default. Stop the other process, or run with another URL: `dotnet run --project src/CRUDTasksWithAgent --urls http://localhost:5290`.
- **HTTPS certificate errors.** Only needed if you use the `https` launch profile. Run `dotnet dev-certs https --trust`.
- **`/_framework/blazor.web.js` returns 404 or the page isn't interactive.** You are probably running with the wrong .NET version. Run `dotnet --version` from the repository root; it should report a 10.x SDK (see `global.json`).
- **Build warnings.** Some lint warnings are expected and deliberate; labs ask you to deal with them. A baseline list is being added at `docs/lint-baseline.md`.
- **Agent page says it isn't configured.** Check the application log; provider construction errors are logged there. See [docs/model-providers.md](docs/model-providers.md).
- **Foundry page shows an info message.** The Foundry agent (`/foundryagent`) is a stretch module: its menu entry appears only when `Features:Foundry` is `true`, and it also needs `FoundryProjectEndpoint` and `FoundryAgentName`. You do not need it for the core labs.

## Deploying to Azure (optional stretch)

The repository includes Bicep templates (`infra/`) and an `azure.yaml` for Azure Developer CLI deployment to App Service with Microsoft Entra authentication. This is not required for any core lab. See [docs/deploy-to-azure.md](docs/deploy-to-azure.md).

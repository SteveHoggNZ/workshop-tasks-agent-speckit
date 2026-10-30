# Model providers for the app agent

The in-app agent (`/agentfx`, Microsoft Agent Framework) talks to a model chosen by the `Llm:Provider` setting. These providers are for the **app agent** only — not for the coding agent you use to work on this repo.

By default (`Scripted`) the agent runs fully offline with no keys: a deterministic model that understands a small command grammar and calls the real task tools.

| `Llm:Provider` | What it uses | Settings |
|---|---|---|
| `Scripted` (default) | Offline scripted model | none |
| `GitHubModels` | `https://models.github.ai/inference` | `Llm:ApiKey` or env `GITHUB_TOKEN`; `Llm:Model` (default `openai/gpt-4.1-mini`) |
| `Ollama` | Local Ollama, OpenAI-compatible API | `Llm:Endpoint` (default `http://localhost:11434/v1`); `Llm:Model` (default `qwen2.5:7b`) |
| `OpenAI` | OpenAI API | `Llm:ApiKey`, `Llm:Model` |
| `AzureOpenAI` | Azure OpenAI | `AzureOpenAIEndpoint`, `ModelDeployment`, optional `AzureOpenAIApiKey` (otherwise `DefaultAzureCredential`) |

## Switching provider

Environment variables (use `__` for `:`):

```bash
export Llm__Provider=GitHubModels
export GITHUB_TOKEN=<token>
dotnet run --project src/CRUDTasksWithAgent --launch-profile http
```

Or user-secrets (keeps keys out of the repo):

```bash
cd src/CRUDTasksWithAgent
dotnet user-secrets set "Llm:Provider" "OpenAI"
dotnet user-secrets set "Llm:ApiKey" "<key>"
dotnet user-secrets set "Llm:Model" "gpt-4.1-mini"
```

## Scripted commands

| Say | Does |
|---|---|
| `add task <title>` / `create task <title>` | create a task |
| `list tasks` / `show tasks` | list tasks |
| `complete <id>` / `done <id>` | mark a task complete |
| `rename <id> to <title>` | change a title |
| `delete <id>` / `remove <id>` | delete a task |

Anything else returns the help text. The commands live in one table: `src/CRUDTasksWithAgent/Services/Scripted/ScriptedIntents.cs`.

If the agent page says it isn't configured, check the application log — provider construction errors are logged there.

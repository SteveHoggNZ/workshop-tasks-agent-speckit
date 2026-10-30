# Task Agent Specification

## Purpose

Describes the in-app task agent, available at `/agentfx` (the "Microsoft Agent Framework Agent" page): the four task tools it exposes and the deterministic scripted model that drives it by default so the lab works offline without any LLM credentials.

## Requirements

### Requirement: Agent tools
The agent SHALL expose exactly four tools for managing tasks: `CreateTask`, `ReadTasks`, `UpdateTask` and `DeleteTask`. All tools SHALL operate on the same task store as the task list page and the REST API.

#### Scenario: Tool set
- **WHEN** the agent is created
- **THEN** it offers `CreateTask`, `ReadTasks`, `UpdateTask` and `DeleteTask` as callable tools

### Requirement: CreateTask tool
`CreateTask` SHALL accept a `title` and an optional `isComplete` flag (default false), store a new task with those values, and return the text "Task created: {title}".

#### Scenario: Create an incomplete task
- **WHEN** `CreateTask` is called with title "Water plants"
- **THEN** an incomplete task titled "Water plants" is stored
- **AND** the result is "Task created: Water plants"

#### Scenario: Create a completed task
- **WHEN** `CreateTask` is called with title "Water plants" and `isComplete` true
- **THEN** the stored task is complete

### Requirement: ReadTasks tool
`ReadTasks` SHALL accept an optional `id`. When `id` is a valid integer it SHALL return a list containing only the matching task, or an empty list if none matches. Otherwise it SHALL return all tasks.

#### Scenario: Read all tasks
- **WHEN** `ReadTasks` is called with no id
- **THEN** every task is returned

#### Scenario: Read one task
- **WHEN** `ReadTasks` is called with id "3" and task 3 exists
- **THEN** a list containing only task 3 is returned

#### Scenario: Read an unknown task
- **WHEN** `ReadTasks` is called with an id that matches no task
- **THEN** an empty list is returned

### Requirement: UpdateTask tool
`UpdateTask` SHALL accept a required `id` plus an optional `title` and an optional `isComplete`. It SHALL change only the fields supplied (a blank title SHALL be ignored) and return "Task {id} updated.". It SHALL return "Invalid task id." when `id` is not an integer and "Task with Id {id} not found." when no task has that id.

#### Scenario: Mark a task complete
- **WHEN** `UpdateTask` is called with id "2" and `isComplete` true
- **THEN** task 2 is stored as complete with its title unchanged
- **AND** the result is "Task 2 updated."

#### Scenario: Rename a task
- **WHEN** `UpdateTask` is called with id "2" and title "New name"
- **THEN** task 2 has the title "New name" and its completion state is unchanged

#### Scenario: Non-numeric id
- **WHEN** `UpdateTask` is called with id "abc"
- **THEN** the result is "Invalid task id." and no task changes

#### Scenario: Unknown id
- **WHEN** `UpdateTask` is called with an id that matches no task
- **THEN** the result is "Task with Id {id} not found."

### Requirement: DeleteTask tool
`DeleteTask` SHALL accept an `id`, remove the matching task, and return "Task {id} deleted.". It SHALL return "Invalid task id." when `id` is not an integer and "Task with Id {id} not found." when no task has that id.

#### Scenario: Delete a task
- **WHEN** `DeleteTask` is called with id "4" and task 4 exists
- **THEN** task 4 is removed
- **AND** the result is "Task 4 deleted."

#### Scenario: Unknown id
- **WHEN** `DeleteTask` is called with an id that matches no task
- **THEN** the result is "Task with Id {id} not found."

### Requirement: Scripted model is the default
When the `Llm:Provider` configuration value is absent or blank, the agent SHALL use the deterministic scripted model, which makes no network calls. Other providers (`GitHubModels`, `Ollama`, `OpenAI`, `AzureOpenAI`) SHALL be used only when explicitly configured, and an unknown provider name SHALL be reported as a configuration error.

#### Scenario: Default provider
- **WHEN** the application runs with no `Llm:Provider` configured
- **THEN** the agent answers using the scripted model

### Requirement: Scripted commands
The scripted model SHALL match the user's latest message, case-insensitively, against a fixed table of commands (add, list, complete, rename, delete) and respond by calling the corresponding tool. The first matching command wins.

#### Scenario: Add a task
- **WHEN** the user sends "add task Buy milk" (or "create task Buy milk"; "a" before "task" and a colon after it are optional)
- **THEN** `CreateTask` is called with title "Buy milk"
- **AND** the reply is "Task created: Buy milk"

#### Scenario: List tasks
- **WHEN** the user sends "list tasks" or "show tasks" ("all" or "my" and a singular "task" are optional variants)
- **THEN** `ReadTasks` is called with no id

#### Scenario: Complete a task by id
- **WHEN** the user sends "complete 12" or "done 12" and task 12 exists ("task" and "#" before the id are optional)
- **THEN** `UpdateTask` is called with id "12" and `isComplete` true
- **AND** task 12 becomes complete

#### Scenario: Rename a task
- **WHEN** the user sends "rename 3 to Write report" ("task" and "#" before the id are optional)
- **THEN** `UpdateTask` is called with id "3" and title "Write report"

#### Scenario: Delete a task by id
- **WHEN** the user sends "delete #4" or "remove 4" ("task" before the id is optional)
- **THEN** `DeleteTask` is called with id "4"
- **AND** the reply is "Task 4 deleted."

### Requirement: Scripted task listing format
After `ReadTasks` returns, the scripted model SHALL reply with the line "Here are your tasks:" followed by one line per task in the form `{id}. [x] {title}` for completed tasks or `{id}. [ ] {title}` for incomplete ones. When there are no tasks it SHALL reply "You have no tasks.".

#### Scenario: Listing tasks
- **WHEN** the user sends "list tasks" and task 1 ("Book room") is complete and task 2 ("Review roadmap") is not
- **THEN** the reply is "Here are your tasks:" followed by the lines "1. [x] Book room" and "2. [ ] Review roadmap"

#### Scenario: Listing with no tasks
- **WHEN** the user sends "list tasks" and no tasks exist
- **THEN** the reply is "You have no tasks."

### Requirement: Scripted help response
When the user's message matches none of the scripted commands, the scripted model SHALL call no tool and reply with a help text that begins "I'm the scripted offline model. I understand these commands:" followed by one usage line per command (`add task <title>`, `list tasks`, `complete <id>`, `rename <id> to <title>`, `delete <id>`).

#### Scenario: Unrecognised message
- **WHEN** the user sends "what's the weather?"
- **THEN** no tool is called
- **AND** the reply is the help text listing the five commands

### Requirement: Tool results as replies
For commands without a custom summary (add, complete, rename, delete), the scripted model SHALL reply with the tool's result text unchanged.

#### Scenario: Result passthrough
- **WHEN** the user sends "delete 999" and no task 999 exists
- **THEN** the reply is "Task with Id 999 not found."

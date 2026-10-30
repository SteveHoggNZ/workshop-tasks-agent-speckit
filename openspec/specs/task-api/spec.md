# Task API Specification

## Purpose

Describes the REST contract under `/api/tasks` that external clients and agents use to manage the same tasks shown in the task list page. The contract is intentionally minimal: it performs no input validation beyond what the ASP.NET Core framework does when binding parameters.

## Requirements

### Requirement: List tasks
`GET /api/tasks` SHALL return status 200 with a JSON array of all tasks ordered by ascending `Id`. Each task SHALL have the properties `id`, `title` and `isComplete`.

#### Scenario: Tasks exist
- **WHEN** a client sends `GET /api/tasks` and three tasks exist
- **THEN** the response status is 200
- **AND** the body is an array of three tasks ordered by ascending `id`

#### Scenario: No tasks
- **WHEN** a client sends `GET /api/tasks` and no tasks exist
- **THEN** the response status is 200
- **AND** the body is an empty array

### Requirement: Get a task by id
`GET /api/tasks/{id}` SHALL return status 200 with the matching task, or status 404 when no task has that id.

#### Scenario: Task found
- **WHEN** a client sends `GET /api/tasks/2` and task 2 exists
- **THEN** the response status is 200
- **AND** the body is task 2

#### Scenario: Task not found
- **WHEN** a client sends `GET /api/tasks/999` and no task has id 999
- **THEN** the response status is 404

### Requirement: Create a task
`POST /api/tasks` SHALL take the task title from the `title` query-string parameter (not from a request body), create an incomplete task with that title, and return status 201 with a `Location` header of `/api/tasks/{id}` and the created task in the body. The endpoint SHALL NOT validate the title: an empty or whitespace-only value is accepted and stored as given.

#### Scenario: Create with a title
- **WHEN** a client sends `POST /api/tasks?title=Buy%20milk`
- **THEN** the response status is 201
- **AND** the `Location` header is `/api/tasks/{id}` for the new task
- **AND** the body is the new task with title "Buy milk" and `isComplete` false

#### Scenario: Empty title is accepted
- **WHEN** a client sends `POST /api/tasks?title=`
- **THEN** the response status is 201
- **AND** a task with an empty title is stored

#### Scenario: Title missing from the query string
- **WHEN** a client sends `POST /api/tasks` with the title only in a JSON body and no `title` query parameter
- **THEN** the request is rejected by parameter binding with status 400
- **AND** no task is created

### Requirement: Update a task
`PUT /api/tasks/{id}` SHALL accept a JSON task body and replace both the `title` and `isComplete` of the existing task with the body's values, then return status 204 with no body. It SHALL return status 404 when no task has that id. Any `id` in the body SHALL be ignored in favour of the id in the URL. The endpoint SHALL NOT validate the body: properties omitted from the body take their default values (empty title, `isComplete` false).

#### Scenario: Update an existing task
- **WHEN** a client sends `PUT /api/tasks/2` with body `{"title":"Renamed","isComplete":true}` and task 2 exists
- **THEN** the response status is 204
- **AND** task 2 now has title "Renamed" and `isComplete` true

#### Scenario: Omitted properties are reset
- **WHEN** a client sends `PUT /api/tasks/2` with body `{"isComplete":true}`
- **THEN** the response status is 204
- **AND** task 2's title is now empty

#### Scenario: Update a missing task
- **WHEN** a client sends `PUT /api/tasks/999` and no task has id 999
- **THEN** the response status is 404

### Requirement: Delete a task
`DELETE /api/tasks/{id}` SHALL remove the task and return status 204 with no body, or return status 404 when no task has that id.

#### Scenario: Delete an existing task
- **WHEN** a client sends `DELETE /api/tasks/2` and task 2 exists
- **THEN** the response status is 204
- **AND** `GET /api/tasks/2` subsequently returns 404

#### Scenario: Delete a missing task
- **WHEN** a client sends `DELETE /api/tasks/999` and no task has id 999
- **THEN** the response status is 404

### Requirement: OpenAPI description
The application SHALL publish an OpenAPI document describing the task endpoints, each with a description and an operation name (`GetAllTasks`, `GetTaskById`, `CreateTask`, `UpdateTask`, `DeleteTask`).

#### Scenario: Operation names
- **WHEN** a client retrieves the OpenAPI document
- **THEN** it contains operations named `GetAllTasks`, `GetTaskById`, `CreateTask`, `UpdateTask` and `DeleteTask`

### Requirement: Shared task store
The API SHALL operate on the same in-memory task store as the task list page, so that changes made through either are visible through the other.

#### Scenario: API change visible in the UI
- **WHEN** a client creates a task through `POST /api/tasks?title=Ship it`
- **THEN** a user loading the task list page sees "Ship it"

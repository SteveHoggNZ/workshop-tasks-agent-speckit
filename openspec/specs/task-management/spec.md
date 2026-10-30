# Task Management Specification

## Purpose

Describes how the Blazor Server task list lets a person view, add, complete and delete tasks, and how the shared task service stores them. The task list page and the in-app agent both rely on this behaviour.

## Requirements

### Requirement: Task model
Each task SHALL have an integer `Id` assigned by the store, a text `Title`, and a boolean `IsComplete` flag. A newly created task SHALL be incomplete unless explicitly created as complete.

#### Scenario: New task defaults
- **WHEN** a task is created with only a title
- **THEN** it is stored with a store-assigned `Id`
- **AND** its `IsComplete` flag is false

### Requirement: Task list page
The application SHALL serve an interactive task list page at `/` titled "Task List" that shows every stored task with a checkbox, the task title, and a Delete button.

#### Scenario: Viewing tasks
- **WHEN** a user opens `/` and tasks exist
- **THEN** one list row is shown per task
- **AND** each row shows a checkbox reflecting the task's completion state, the title, and a Delete button

#### Scenario: No tasks
- **WHEN** a user opens `/` and no tasks exist
- **THEN** no task list is rendered

### Requirement: Task ordering
The task list SHALL show incomplete tasks before completed tasks, and tasks within each group SHALL appear in ascending `Id` order.

#### Scenario: Completed tasks sink to the bottom
- **WHEN** the list contains an incomplete task with a higher `Id` than a completed task
- **THEN** the incomplete task is shown above the completed task

#### Scenario: Completing a task moves it down
- **WHEN** a user marks an incomplete task as complete
- **THEN** the list is reloaded
- **AND** that task appears after all incomplete tasks

### Requirement: Completed task presentation
The task list page SHALL render the title of a completed task struck through in a muted grey colour, and SHALL render incomplete task titles without that styling.

#### Scenario: Completed task styling
- **WHEN** a task is complete
- **THEN** its title is shown with a line-through and grey text

### Requirement: Add a task from the page
The task list page SHALL provide a text input (placeholder "Enter task title") and an "Add Task" button. Activating the button, or pressing Enter in the input, SHALL create a task with the entered title, clear the input, and refresh the list. Blank or whitespace-only titles SHALL be ignored.

#### Scenario: Adding with the button
- **WHEN** a user types "Buy milk" and clicks "Add Task"
- **THEN** a new incomplete task titled "Buy milk" is stored
- **AND** the input is cleared
- **AND** the list shows the new task

#### Scenario: Adding with the Enter key
- **WHEN** a user types a title and presses Enter in the input
- **THEN** the task is created exactly as if "Add Task" had been clicked

#### Scenario: Blank title
- **WHEN** a user clicks "Add Task" with an empty or whitespace-only input
- **THEN** no task is created

### Requirement: Toggle completion from the page
Changing a task's checkbox SHALL persist the new completion state for that task and refresh the list.

#### Scenario: Marking complete
- **WHEN** a user checks the checkbox of an incomplete task
- **THEN** the task is stored as complete

#### Scenario: Marking incomplete
- **WHEN** a user unchecks the checkbox of a completed task
- **THEN** the task is stored as incomplete

### Requirement: Delete a task from the page
Clicking a task's Delete button SHALL remove that task from the store and refresh the list.

#### Scenario: Deleting a task
- **WHEN** a user clicks Delete on a task
- **THEN** that task no longer appears in the list
- **AND** other tasks are unaffected

### Requirement: In-memory storage
Tasks SHALL be held in an in-memory database shared by all users of the running application, and SHALL NOT survive an application restart.

#### Scenario: Restart clears data
- **WHEN** the application is restarted
- **THEN** all tasks created since startup are gone

### Requirement: Demo data seeding
When the application starts in the Development environment with an empty task store, it SHALL seed five demo tasks. It SHALL NOT seed tasks in any other environment, nor when tasks already exist.

#### Scenario: Development startup
- **WHEN** the application starts in the Development environment
- **THEN** five demo tasks exist, two of them complete

#### Scenario: Production startup
- **WHEN** the application starts in a non-Development environment
- **THEN** the task store starts empty

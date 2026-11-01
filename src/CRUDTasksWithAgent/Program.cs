using CRUDTasksWithAgent.Components;
using CRUDTasksWithAgent.Models;
using CRUDTasksWithAgent.Tools;
using CRUDTasksWithAgent.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<TasksDbContext>(options =>
    options.UseInMemoryDatabase("TasksDb"));

builder.Services.AddSingleton(TimeProvider.System);

// Register TaskService and TaskCrudTool as scoped services
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TaskCrudTool>();

// Register the chat client factory (selects the model provider from the Llm config section)
builder.Services.AddSingleton<IChatClientFactory, ChatClientFactory>();

// Register Agent Framework provider (for AgentFrameworkAgent.razor)
builder.Services.AddScoped<IAgentFrameworkProvider, AgentFrameworkProvider>();

// Register Foundry agent provider (for FoundryAgent.razor)
builder.Services.AddScoped<IFoundryAgentProvider, FoundryAgentProvider>();

// Register OpenAPI for external agents
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// API endpoints for TaskItem
app.MapOpenApi();

app.MapGet("/api/tasks", async (string? priority, string? dueBefore, string? overdue, string? sort, TaskService taskService) =>
{
    var errors = new Dictionary<string, string[]>();
    TaskPriority? priorityFilter = null;
    DateOnly? dueFilter = null;
    var overdueOnly = false;

    if (priority is not null)
    {
        if (TaskInputParser.TryParsePriority(priority, out var p)) priorityFilter = p;
        else errors["priority"] = [TaskInputParser.PriorityError];
    }
    if (dueBefore is not null)
    {
        if (TaskInputParser.TryParseDueDate(dueBefore, out var d)) dueFilter = d;
        else errors["dueBefore"] = [TaskInputParser.DueDateError];
    }
    if (overdue is not null && !bool.TryParse(overdue, out overdueOnly))
        errors["overdue"] = ["Overdue must be true or false."];
    if (sort is not null && !sort.Equals("priority", StringComparison.OrdinalIgnoreCase))
        errors["sort"] = ["Sort must be 'priority' or omitted."];
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var urgencyOrder = sort is not null;
    return Results.Ok(await taskService.GetTasksAsync(new TaskQuery(priorityFilter, dueFilter, overdueOnly, urgencyOrder)));
})
.WithDescription("Gets tasks. Optional filters: priority, dueBefore (due on or before, yyyy-MM-dd), overdue. sort=priority returns most urgent first; otherwise creation order.")
.WithName("GetAllTasks");

app.MapGet("/api/tasks/{id}", async (int id, TaskService taskService) =>
{
    var task = await taskService.GetTaskByIdAsync(id);
    return task is not null ? Results.Ok(task) : Results.NotFound();
})
.WithDescription("Gets a task by its ID.")
.WithName("GetTaskById");

app.MapPost("/api/tasks", async (string title, string? priority, string? dueDate, TaskService taskService) =>
{
    var errors = new Dictionary<string, string[]>();
    var taskPriority = TaskPriority.Normal;
    DateOnly? due = null;
    if (priority is not null)
    {
        if (TaskInputParser.TryParsePriority(priority, out var p)) taskPriority = p;
        else errors["priority"] = [TaskInputParser.PriorityError];
    }
    if (dueDate is not null)
    {
        if (TaskInputParser.TryParseDueDate(dueDate, out var d)) due = d;
        else errors["dueDate"] = [TaskInputParser.DueDateError];
    }
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var created = await taskService.AddTaskAsync(title, taskPriority, due);
    return Results.Created($"/api/tasks/{created.Id}", created);
})
.WithDescription("Creates a new task with the specified title and optional priority (Low, Normal, High) and dueDate (yyyy-MM-dd).")
.WithName("CreateTask");

app.MapPut("/api/tasks/{id}", async (int id, UpdateTaskRequest item, TaskService taskService) =>
{
    var task = await taskService.GetTaskByIdAsync(id);
    if (task is null) return Results.NotFound();
    task.Title = item.Title;
    task.IsComplete = item.IsComplete;
    if (item.Priority is { } priority) task.Priority = priority;
    if (item.DueDateSpecified) task.DueDate = item.DueDate;
    await taskService.UpdateTaskAsync(task);
    return Results.NoContent();
})
.WithDescription("Updates an existing task by ID. Omitted priority/dueDate are left unchanged; dueDate null clears the due date.")
.WithName("UpdateTask");

app.MapDelete("/api/tasks/{id}", async (int id, TaskService taskService) =>
{
    var task = await taskService.GetTaskByIdAsync(id);
    if (task is null) return Results.NotFound();
    await taskService.DeleteTaskAsync(task);
    return Results.NoContent();
})
.WithDescription("Deletes a task by its ID.")
.WithName("DeleteTask");

app.SeedDemoData();

app.Run();

public partial class Program { }

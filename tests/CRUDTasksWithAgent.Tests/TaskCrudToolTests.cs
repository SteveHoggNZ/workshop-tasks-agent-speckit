using CRUDTasksWithAgent.Models;
using CRUDTasksWithAgent.Services;
using CRUDTasksWithAgent.Tests.Helpers;
using CRUDTasksWithAgent.Tools;

namespace CRUDTasksWithAgent.Tests;

public class TaskCrudToolTests
{
    private static (TaskCrudTool Tool, TaskService Service) NewTool()
    {
        var service = new TaskService(TestDb.Create());
        return (new TaskCrudTool(service), service);
    }

    [Fact]
    public async Task CreateTask_AddsTaskAndReportsIt()
    {
        var (tool, service) = NewTool();

        var result = await tool.CreateTaskAsync("Water plants");

        Assert.Equal("Task created: Water plants", result);
        var tasks = await service.GetAllTasksAsync();
        var task = Assert.Single(tasks);
        Assert.Equal("Water plants", task.Title);
        Assert.False(task.IsComplete);
    }

    [Fact]
    public async Task CreateTask_CanMarkTaskComplete()
    {
        var (tool, service) = NewTool();

        await tool.CreateTaskAsync("Already done", isComplete: true);

        Assert.True(Assert.Single(await service.GetAllTasksAsync()).IsComplete);
    }

    [Fact]
    public async Task ReadTasks_ReturnsAllTasks()
    {
        var (tool, service) = NewTool();
        await service.AddTaskAsync("one");
        await service.AddTaskAsync("two");

        var tasks = await tool.ReadTasksAsync();

        Assert.Equal(2, tasks.Count);
    }

    [Fact]
    public async Task ReadTasks_ById_ReturnsSingleTask()
    {
        var (tool, service) = NewTool();
        var first = await service.AddTaskAsync("one");
        await service.AddTaskAsync("two");

        var tasks = await tool.ReadTasksAsync(first.Id.ToString());

        Assert.Equal("one", Assert.Single(tasks).Title);
    }

    [Fact]
    public async Task ReadTasks_UnknownId_ReturnsEmpty()
    {
        var (tool, _) = NewTool();

        Assert.Empty(await tool.ReadTasksAsync("9"));
    }

    private static readonly DateOnly Today = new(2026, 11, 1);

    [Fact]
    public async Task CreateTask_StoresPriorityAndDueDate_CaseInsensitive()
    {
        var (tool, service) = NewTool();

        var result = await tool.CreateTaskAsync("Send report", priority: "hIGH", dueDate: "2026-12-01");

        Assert.StartsWith("Task created: Send report", result);
        var task = Assert.Single(await service.GetAllTasksAsync());
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(new DateOnly(2026, 12, 1), task.DueDate);
    }

    [Theory]
    [InlineData("urgent", null)]
    [InlineData(null, "2026-02-30")]
    [InlineData(null, "tomorrow")]
    public async Task CreateTask_InvalidPriorityOrDate_ReturnsMessageAndCreatesNothing(string? priority, string? dueDate)
    {
        var (tool, service) = NewTool();

        var result = await tool.CreateTaskAsync("Nope", priority: priority, dueDate: dueDate);

        Assert.Contains(priority is not null ? "Priority" : "Due date", result);
        Assert.Empty(await service.GetAllTasksAsync());
    }

    [Fact]
    public async Task UpdateTask_SetsPriorityThenDueDate_ThenClearsDueDate_ForIdTenOrHigher()
    {
        var (tool, service) = NewTool();
        for (var i = 0; i < 11; i++) await service.AddTaskAsync($"task {i}");
        var target = (await service.GetAllTasksAsync())[10];
        Assert.True(target.Id >= 10);

        await tool.UpdateTaskAsync($"{target.Id}", priority: "low");
        await tool.UpdateTaskAsync($"{target.Id}", dueDate: "2026-12-05");
        var set = (await service.GetTaskByIdAsync(target.Id))!;
        Assert.Equal(TaskPriority.Low, set.Priority);
        Assert.Equal(new DateOnly(2026, 12, 5), set.DueDate);
        Assert.Equal("task 10", set.Title);

        await tool.UpdateTaskAsync($"{target.Id}", clearDueDate: true);
        var cleared = (await service.GetTaskByIdAsync(target.Id))!;
        Assert.Null(cleared.DueDate);
        Assert.Equal(TaskPriority.Low, cleared.Priority);
    }

    [Fact]
    public async Task UpdateTask_InvalidInput_ReturnsMessageAndChangesNothing()
    {
        var (tool, service) = NewTool();
        var task = await service.AddTaskAsync("keep", TaskPriority.High, Today);
        var id = $"{task.Id}";

        Assert.Contains("Priority", await tool.UpdateTaskAsync(id, priority: "urgent"));
        Assert.Contains("Due date", await tool.UpdateTaskAsync(id, dueDate: "2026-02-30"));
        Assert.Contains("clear", await tool.UpdateTaskAsync(id, dueDate: "2026-12-01", clearDueDate: true), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not found", await tool.UpdateTaskAsync("999", priority: "low"), StringComparison.OrdinalIgnoreCase);

        var saved = (await service.GetTaskByIdAsync(task.Id))!;
        Assert.Equal(TaskPriority.High, saved.Priority);
        Assert.Equal(Today, saved.DueDate);
    }

    [Fact]
    public async Task ReadTasks_ReturnsUrgencyOrder()
    {
        var (tool, service) = NewTool();
        await service.AddTaskAsync("low", TaskPriority.Low);
        await service.AddTaskAsync("high", TaskPriority.High);

        var tasks = await tool.ReadTasksAsync();

        Assert.Equal(["high", "low"], tasks.Select(t => t.Title).ToList());
    }

    [Fact]
    public async Task ReadTasks_OverdueOnly_ReturnsOnlyIncompleteTasksDueBeforeToday()
    {
        var service = new TaskService(TestDb.Create(), new FixedTimeProvider(Today));
        var tool = new TaskCrudTool(service);
        await service.AddTaskAsync("overdue", dueDate: Today.AddDays(-1));
        await service.AddTaskAsync("due today", dueDate: Today);
        var done = await service.AddTaskAsync("done", dueDate: Today.AddDays(-4));
        await service.SetTaskCompletionAsync(done, true);

        var tasks = await tool.ReadTasksAsync(overdueOnly: true);

        Assert.Equal("overdue", Assert.Single(tasks).Title);
    }
}

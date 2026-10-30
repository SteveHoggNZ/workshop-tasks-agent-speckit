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
}

using CRUDTasksWithAgent.Services;
using CRUDTasksWithAgent.Tests.Helpers;

namespace CRUDTasksWithAgent.Tests;

public class TaskServiceTests
{
    private static TaskService NewService() => new(TestDb.Create());

    [Fact]
    public async Task AddTask_PersistsTaskWithTitle()
    {
        var service = NewService();

        var created = await service.AddTaskAsync("Write tests");

        Assert.True(created.Id > 0);
        Assert.Equal("Write tests", created.Title);
        Assert.False(created.IsComplete);
    }

    [Fact]
    public async Task GetAllTasks_ReturnsTasksOrderedById()
    {
        var service = NewService();
        await service.AddTaskAsync("first");
        await service.AddTaskAsync("second");

        var all = await service.GetAllTasksAsync();

        Assert.Equal(new[] { "first", "second" }, all.Select(t => t.Title));
    }

    [Fact]
    public async Task GetTaskById_ReturnsNullWhenMissing()
    {
        var service = NewService();

        Assert.Null(await service.GetTaskByIdAsync(42));
    }

    [Fact]
    public async Task UpdateTask_SavesChanges()
    {
        var service = NewService();
        var task = await service.AddTaskAsync("old");

        task.Title = "new";
        await service.UpdateTaskAsync(task);

        Assert.Equal("new", (await service.GetTaskByIdAsync(task.Id))!.Title);
    }

    [Fact]
    public async Task SetTaskCompletion_TogglesFlag()
    {
        var service = NewService();
        var task = await service.AddTaskAsync("toggle me");

        await service.SetTaskCompletionAsync(task, true);

        Assert.True((await service.GetTaskByIdAsync(task.Id))!.IsComplete);
    }

    [Fact]
    public async Task DeleteTask_RemovesTask()
    {
        var service = NewService();
        var task = await service.AddTaskAsync("temp");

        await service.DeleteTaskAsync(task);

        Assert.Empty(await service.GetAllTasksAsync());
    }
}

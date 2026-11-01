using CRUDTasksWithAgent.Models;
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

    private static readonly DateOnly Today = new(2026, 11, 1);

    private static TaskService NewService(FixedTimeProvider clock) => new(TestDb.Create(), clock);

    [Fact]
    public async Task AddTask_DefaultsToNormalPriorityAndNoDueDate()
    {
        var created = await NewService().AddTaskAsync("defaults");

        Assert.Equal(TaskPriority.Normal, created.Priority);
        Assert.Null(created.DueDate);
    }

    [Fact]
    public async Task AddTask_StoresPriorityAndDueDate()
    {
        var created = await NewService().AddTaskAsync("urgent", TaskPriority.High, Today.AddDays(2));

        Assert.Equal(TaskPriority.High, created.Priority);
        Assert.Equal(Today.AddDays(2), created.DueDate);
    }

    [Fact]
    public async Task UpdateTask_SetsAndClearsPriorityAndDueDate()
    {
        var service = NewService();
        var task = await service.AddTaskAsync("change me");

        task.Priority = TaskPriority.Low;
        task.DueDate = Today;
        await service.UpdateTaskAsync(task);
        var saved = (await service.GetTaskByIdAsync(task.Id))!;
        Assert.Equal(TaskPriority.Low, saved.Priority);
        Assert.Equal(Today, saved.DueDate);

        saved.DueDate = null;
        await service.UpdateTaskAsync(saved);
        Assert.Null((await service.GetTaskByIdAsync(task.Id))!.DueDate);
    }

    [Fact]
    public async Task GetTasks_DefaultOrder_IncompleteFirstThenPriorityThenDueDateThenTitle()
    {
        var service = NewService(new FixedTimeProvider(Today));
        await service.AddTaskAsync("b normal no date");
        await service.AddTaskAsync("a normal no date");
        await service.AddTaskAsync("normal later", TaskPriority.Normal, Today.AddDays(5));
        await service.AddTaskAsync("normal sooner", TaskPriority.Normal, Today.AddDays(1));
        await service.AddTaskAsync("low", TaskPriority.Low, Today);
        await service.AddTaskAsync("high no date", TaskPriority.High);
        await service.AddTaskAsync("high dated", TaskPriority.High, Today.AddDays(9));
        var done = await service.AddTaskAsync("done high", TaskPriority.High, Today);
        await service.SetTaskCompletionAsync(done, true);
        var done2 = await service.AddTaskAsync("done low", TaskPriority.Low);
        await service.SetTaskCompletionAsync(done2, true);

        var ordered = await service.GetTasksAsync();

        Assert.Equal(
            [
                "high dated", "high no date", "normal sooner", "normal later",
                "a normal no date", "b normal no date", "low", "done high", "done low"
            ],
            ordered.Select(t => t.Title).ToList());
    }

    [Fact]
    public async Task GetTasks_SamePriorityDateAndTitle_FallsBackToId()
    {
        var service = NewService();
        var first = await service.AddTaskAsync("same");
        var second = await service.AddTaskAsync("same");

        var ordered = await service.GetTasksAsync();

        Assert.Equal(new[] { first.Id, second.Id }, ordered.Select(t => t.Id));
    }

    [Fact]
    public async Task GetTasks_WithoutUrgencyOrder_ReturnsIdOrder()
    {
        var service = NewService();
        await service.AddTaskAsync("first", TaskPriority.Low);
        await service.AddTaskAsync("second", TaskPriority.High);

        var ordered = await service.GetTasksAsync(new TaskQuery(UrgencyOrder: false));

        Assert.Equal(["first", "second"], ordered.Select(t => t.Title).ToList());
    }

    [Fact]
    public async Task IsOverdue_OnlyForIncompleteTasksDueBeforeToday()
    {
        var clock = new FixedTimeProvider(Today);
        var service = NewService(clock);
        var past = await service.AddTaskAsync("past", dueDate: Today.AddDays(-1));
        var dueToday = await service.AddTaskAsync("today", dueDate: Today);
        var noDate = await service.AddTaskAsync("none");
        var donePast = await service.AddTaskAsync("done past", dueDate: Today.AddDays(-3));
        await service.SetTaskCompletionAsync(donePast, true);

        Assert.True(service.IsOverdue(past));
        Assert.False(service.IsOverdue(dueToday));
        Assert.False(service.IsOverdue(noDate));
        Assert.False(service.IsOverdue(donePast));

        clock.Today = Today.AddDays(1);
        Assert.True(service.IsOverdue(dueToday));
    }

    [Fact]
    public async Task GetTasks_FilterByPriority()
    {
        var service = NewService();
        await service.AddTaskAsync("low", TaskPriority.Low);
        await service.AddTaskAsync("high", TaskPriority.High);

        var result = await service.GetTasksAsync(new TaskQuery(Priority: TaskPriority.High));

        Assert.Equal("high", Assert.Single(result).Title);
    }

    [Fact]
    public async Task GetTasks_FilterDueOnOrBefore_IsInclusiveAndExcludesUndated()
    {
        var service = NewService();
        await service.AddTaskAsync("early", dueDate: Today.AddDays(-1));
        await service.AddTaskAsync("on the day", dueDate: Today);
        await service.AddTaskAsync("late", dueDate: Today.AddDays(1));
        await service.AddTaskAsync("undated");

        var result = await service.GetTasksAsync(new TaskQuery(DueOnOrBefore: Today));

        Assert.Equal(["early", "on the day"], result.Select(t => t.Title).Order().ToList());
    }

    [Fact]
    public async Task GetTasks_FilterOverdue_ExcludesCompleted()
    {
        var service = NewService(new FixedTimeProvider(Today));
        await service.AddTaskAsync("overdue", dueDate: Today.AddDays(-2));
        await service.AddTaskAsync("not yet", dueDate: Today);
        var done = await service.AddTaskAsync("done", dueDate: Today.AddDays(-2));
        await service.SetTaskCompletionAsync(done, true);

        var result = await service.GetTasksAsync(new TaskQuery(OverdueOnly: true));

        Assert.Equal("overdue", Assert.Single(result).Title);
    }

    [Fact]
    public async Task GetTasks_CombinedFilters_UseAnd_AndEmptyResultIsNotAnError()
    {
        var service = NewService(new FixedTimeProvider(Today));
        await service.AddTaskAsync("high overdue", TaskPriority.High, Today.AddDays(-1));
        await service.AddTaskAsync("low overdue", TaskPriority.Low, Today.AddDays(-1));
        await service.AddTaskAsync("high later", TaskPriority.High, Today.AddDays(4));

        var result = await service.GetTasksAsync(new TaskQuery(TaskPriority.High, Today.AddDays(10), OverdueOnly: true));
        Assert.Equal("high overdue", Assert.Single(result).Title);

        var none = await service.GetTasksAsync(new TaskQuery(TaskPriority.Low, Today.AddDays(-5), OverdueOnly: true));
        Assert.Empty(none);
    }
}

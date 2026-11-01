using CRUDTasksWithAgent.Models;
using CRUDTasksWithAgent.Services;
using CRUDTasksWithAgent.Services.Scripted;
using CRUDTasksWithAgent.Tests.Helpers;
using CRUDTasksWithAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRUDTasksWithAgent.Tests;

public class ScriptedModelTests
{
    private static async Task<FunctionCallContent> FirstCall(string userText)
    {
        var client = new ScriptedChatClient();
        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, userText)]);
        return Assert.IsType<FunctionCallContent>(Assert.Single(response.Messages.Single().Contents));
    }

    [Fact]
    public async Task Add_CallsCreateTask()
    {
        var call = await FirstCall("add task Buy milk");

        Assert.Equal("CreateTask", call.Name);
        Assert.Equal("Buy milk", call.Arguments!["title"]);
    }

    [Fact]
    public async Task Create_IsCaseInsensitiveAlias()
    {
        var call = await FirstCall("CREATE TASK Walk dog");

        Assert.Equal("CreateTask", call.Name);
        Assert.Equal("Walk dog", call.Arguments!["title"]);
    }

    [Theory]
    [InlineData("list tasks")]
    [InlineData("Show tasks")]
    public async Task List_CallsReadTasks(string text)
    {
        var call = await FirstCall(text);

        Assert.Equal("ReadTasks", call.Name);
        Assert.Empty(call.Arguments!);
    }

    [Theory]
    [InlineData("complete 3")]
    [InlineData("done 3")]
    public async Task Complete_CallsUpdateTaskWithIsComplete(string text)
    {
        var call = await FirstCall(text);

        Assert.Equal("UpdateTask", call.Name);
        Assert.Equal("3", call.Arguments!["id"]);
        Assert.Equal(true, call.Arguments["isComplete"]);
    }

    [Fact]
    public async Task Rename_CallsUpdateTaskWithTitle()
    {
        var call = await FirstCall("rename 2 to Call mum");

        Assert.Equal("UpdateTask", call.Name);
        Assert.Equal("2", call.Arguments!["id"]);
        Assert.Equal("Call mum", call.Arguments["title"]);
    }

    [Theory]
    [InlineData("delete 4")]
    [InlineData("remove 4")]
    public async Task Delete_CallsDeleteTask(string text)
    {
        var call = await FirstCall(text);

        Assert.Equal("DeleteTask", call.Name);
        Assert.Equal("4", call.Arguments!["id"]);
    }

    [Fact]
    public async Task Add_WithPriorityAndDueSuffixes_PassesThemToCreateTask()
    {
        var call = await FirstCall("add task Send report priority high due 2026-12-01");

        Assert.Equal("CreateTask", call.Name);
        Assert.Equal("Send report", call.Arguments!["title"]);
        Assert.Equal("high", call.Arguments["priority"]);
        Assert.Equal("2026-12-01", call.Arguments["dueDate"]);
    }

    [Fact]
    public async Task Add_WithoutSuffixes_IsUnchanged()
    {
        var call = await FirstCall("add task Buy milk");

        Assert.Equal(["title"], call.Arguments!.Keys.ToList());
    }

    [Fact]
    public async Task Add_TitleContainingTheWordDue_KeepsItInTheTitle()
    {
        var call = await FirstCall("add task Pay the due diligence invoice");

        Assert.Equal("Pay the due diligence invoice", call.Arguments!["title"]);
        Assert.False(call.Arguments.ContainsKey("dueDate"));
    }

    [Theory]
    [InlineData("priority 12 low", "priority", "low")]
    [InlineData("due 12 2026-12-01", "dueDate", "2026-12-01")]
    public async Task PriorityAndDue_CallUpdateTask_ForMultiDigitIds(string text, string key, string value)
    {
        var call = await FirstCall(text);

        Assert.Equal("UpdateTask", call.Name);
        Assert.Equal("12", call.Arguments!["id"]);
        Assert.Equal(value, call.Arguments[key]);
    }

    [Fact]
    public async Task ClearDue_CallsUpdateTaskWithClearDueDate()
    {
        var call = await FirstCall("clear due 12");

        Assert.Equal("UpdateTask", call.Name);
        Assert.Equal("12", call.Arguments!["id"]);
        Assert.Equal(true, call.Arguments["clearDueDate"]);
    }

    [Fact]
    public async Task ShowOverdue_CallsReadTasksWithOverdueOnly()
    {
        var call = await FirstCall("show overdue");

        Assert.Equal("ReadTasks", call.Name);
        Assert.Equal(true, call.Arguments!["overdueOnly"]);
    }

    [Fact]
    public async Task UnparsableDateOrPriority_IsPassedThroughForTheToolToReject()
    {
        var call = await FirstCall("priority 3 urgent");

        Assert.Equal("urgent", call.Arguments!["priority"]);
    }

    private static async Task<string> Summary(string userText, string toolName, object? result)
    {
        var call = new FunctionCallContent("c1", toolName, new Dictionary<string, object?>());
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, userText),
            new(ChatRole.Assistant, [call]),
            new(ChatRole.Tool, [new FunctionResultContent("c1", result)]),
        };
        return (await new ScriptedChatClient().GetResponseAsync(messages)).Text;
    }

    [Fact]
    public async Task ListReply_ShowsPriorityAndDueDate_AndOmitsDueWhenThereIsNone()
    {
        var tasks = new List<TaskItem>
        {
            new() { Id = 7, Title = "Send report", Priority = TaskPriority.High, DueDate = new DateOnly(2026, 12, 1) },
            new() { Id = 8, Title = "Tidy desk", IsComplete = true },
        };

        var text = await Summary("list tasks", "ReadTasks", tasks);

        Assert.Contains("7. [ ] Send report (High, due 2026-12-01)", text);
        Assert.Contains("8. [x] Tidy desk (Normal)", text);
    }

    [Fact]
    public async Task OverdueReply_SaysNothingIsOverdueWhenEmpty()
    {
        Assert.Equal("Nothing is overdue.", await Summary("show overdue", "ReadTasks", new List<TaskItem>()));
        Assert.Equal("You have no tasks.", await Summary("list tasks", "ReadTasks", new List<TaskItem>()));
    }

    [Fact]
    public async Task EndToEnd_ChatSetsPriorityAndDueDate_AndListsMostUrgentFirst()
    {
        var dbName = $"e2e-urgency-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Llm:Provider"] = "Scripted" })
            .Build());
        services.AddDbContext<TasksDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<TaskService>();
        services.AddScoped<TaskCrudTool>();
        services.AddSingleton<IChatClientFactory, ChatClientFactory>();
        services.AddScoped<IAgentFrameworkProvider, AgentFrameworkProvider>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var agentProvider = scope.ServiceProvider.GetRequiredService<IAgentFrameworkProvider>();
        var agent = Assert.IsAssignableFrom<AIAgent>(agentProvider.Agent);
        var thread = agentProvider.Thread;
        var service = scope.ServiceProvider.GetRequiredService<TaskService>();

        await agent.RunAsync("add task Water plants", thread);
        await agent.RunAsync("add task File taxes priority high due 2020-01-31", thread);
        await agent.RunAsync("priority 1 low", thread);
        await agent.RunAsync("due 1 2099-01-01", thread);
        await agent.RunAsync("clear due 1", thread);

        var first = (await service.GetTaskByIdAsync(1))!;
        Assert.Equal(TaskPriority.Low, first.Priority);
        Assert.Null(first.DueDate);

        var listed = await agent.RunAsync("list tasks", thread);
        Assert.True(listed.Text.IndexOf("File taxes", StringComparison.Ordinal) < listed.Text.IndexOf("Water plants", StringComparison.Ordinal));

        var overdue = await agent.RunAsync("show overdue", thread);
        Assert.Contains("File taxes", overdue.Text);
        Assert.DoesNotContain("Water plants", overdue.Text);
    }

    [Fact]
    public async Task Unknown_ReturnsHelpText()
    {
        var client = new ScriptedChatClient();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "what is the weather?")]);

        Assert.Equal(ScriptedIntents.HelpText, response.Text);
        foreach (var intent in ScriptedIntents.All)
        {
            Assert.Contains(intent.Usage, response.Text);
        }
    }

    [Fact]
    public async Task ToolResult_ProducesSummary()
    {
        var client = new ScriptedChatClient();
        var call = new FunctionCallContent("c1", "CreateTask", new Dictionary<string, object?> { ["title"] = "X" });
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "add task X"),
            new(ChatRole.Assistant, [call]),
            new(ChatRole.Tool, [new FunctionResultContent("c1", "Task created: X")]),
        };

        var response = await client.GetResponseAsync(messages);

        Assert.Equal("Task created: X", response.Text);
    }

    [Fact]
    public async Task Suggestions_EachExampleRoutesToItsOwnTool()
    {
        foreach (var intent in ScriptedIntents.All)
        {
            var call = await FirstCall(intent.Example);

            Assert.Equal(intent.ToolName, call.Name);
            Assert.Contains(intent.Example, ScriptedIntents.Suggestions);
        }
    }

    [Fact]
    public async Task Suggestions_HelpReturnsHelpText()
    {
        Assert.Contains("help", ScriptedIntents.Suggestions);

        var response = await new ScriptedChatClient().GetResponseAsync([new ChatMessage(ChatRole.User, "help")]);

        Assert.Equal(ScriptedIntents.HelpText, response.Text);
    }

    [Fact]
    public void IntentToolNames_MatchRegisteredTools()
    {
        var tool = new TaskCrudTool(new TaskService(TestDb.Create()));
        var registered = new[]
        {
            AIFunctionFactory.Create(tool.CreateTaskAsync).Name,
            AIFunctionFactory.Create(tool.ReadTasksAsync).Name,
            AIFunctionFactory.Create(tool.UpdateTaskAsync).Name,
            AIFunctionFactory.Create(tool.DeleteTaskAsync).Name,
        };

        foreach (var intent in ScriptedIntents.All)
        {
            Assert.Contains(intent.ToolName, registered);
        }
    }

    [Fact]
    public async Task EndToEnd_AgentRunsRealToolsAgainstTaskService()
    {
        var dbName = $"e2e-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Llm:Provider"] = "Scripted" })
            .Build());
        services.AddDbContext<TasksDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<TaskService>();
        services.AddScoped<TaskCrudTool>();
        services.AddSingleton<IChatClientFactory, ChatClientFactory>();
        services.AddScoped<IAgentFrameworkProvider, AgentFrameworkProvider>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var agentProvider = scope.ServiceProvider.GetRequiredService<IAgentFrameworkProvider>();
        var agent = Assert.IsAssignableFrom<AIAgent>(agentProvider.Agent);
        var thread = agentProvider.Thread;
        var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();

        await agent.RunAsync("add task Write docs", thread);
        var created = Assert.Single(await taskService.GetAllTasksAsync());
        Assert.Equal("Write docs", created.Title);
        Assert.Equal(1, created.Id);

        var listed = await agent.RunAsync("list tasks", thread);
        Assert.Contains("Write docs", listed.Text);

        await agent.RunAsync("complete 1", thread);
        Assert.True((await taskService.GetTaskByIdAsync(1))!.IsComplete);

        await agent.RunAsync("delete 1", thread);
        Assert.Empty(await taskService.GetAllTasksAsync());
    }
}

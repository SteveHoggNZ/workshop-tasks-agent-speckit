using System.Net;
using System.Net.Http.Json;
using CRUDTasksWithAgent.Models;
using CRUDTasksWithAgent.Tests.Helpers;

namespace CRUDTasksWithAgent.Tests;

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<TaskItem> CreateAsync(string title, string? priority = null, string? dueDate = null)
    {
        var url = $"/api/tasks?title={Uri.EscapeDataString(title)}";
        if (priority is not null) url += $"&priority={priority}";
        if (dueDate is not null) url += $"&dueDate={dueDate}";
        var response = await _client.PostAsync(url, null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskItem>())!;
    }

    [Fact]
    public async Task Post_CreatesTaskAndReturnsLocation()
    {
        var response = await _client.PostAsync("/api/tasks?title=Buy%20milk", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskItem>();
        Assert.Equal("Buy milk", created!.Title);
        Assert.EndsWith($"/api/tasks/{created.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task GetAll_ReturnsCreatedTasks()
    {
        var created = await CreateAsync("listed task");

        var all = await _client.GetFromJsonAsync<List<TaskItem>>("/api/tasks");

        Assert.Contains(all!, t => t.Id == created.Id && t.Title == "listed task");
    }

    [Fact]
    public async Task GetById_ReturnsTask()
    {
        var created = await CreateAsync("by id");

        var response = await _client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItem>();
        Assert.Equal("by id", task!.Title);
    }

    [Fact]
    public async Task GetById_Returns404WhenMissing()
    {
        var response = await _client.GetAsync("/api/tasks/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesTask()
    {
        var created = await CreateAsync("before");

        var response = await _client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new TaskItem { Title = "after", IsComplete = true });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var task = await _client.GetFromJsonAsync<TaskItem>($"/api/tasks/{created.Id}");
        Assert.Equal("after", task!.Title);
        Assert.True(task.IsComplete);
    }

    [Fact]
    public async Task Put_Returns404WhenMissing()
    {
        var response = await _client.PutAsJsonAsync("/api/tasks/9999", new TaskItem { Title = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        var created = await CreateAsync("to delete");

        var response = await _client.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/tasks/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404WhenMissing()
    {
        var response = await _client.DeleteAsync("/api/tasks/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string Date(int daysFromToday) =>
        DateOnly.FromDateTime(DateTime.Now).AddDays(daysFromToday).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private Task<HttpResponseMessage> PutJson(int id, string json) =>
        _client.PutAsync($"/api/tasks/{id}", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

    [Fact]
    public async Task Post_DefaultsToNormalAndNoDueDate()
    {
        var created = await CreateAsync("plain");

        Assert.Equal(TaskPriority.Normal, created.Priority);
        Assert.Null(created.DueDate);
    }

    [Fact]
    public async Task Post_AcceptsPriorityInAnyCase_AndReturnsCanonicalJson()
    {
        var response = await _client.PostAsync($"/api/tasks?title=cased&priority=hIgH&dueDate={Date(3)}", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var json = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal("High", json.RootElement.GetProperty("priority").GetString());
        Assert.Equal(Date(3), json.RootElement.GetProperty("dueDate").GetString());
    }

    [Theory]
    [InlineData("priority=urgent")]
    [InlineData("priority=2")]
    [InlineData("dueDate=2026-02-30")]
    [InlineData("dueDate=01/02/2026")]
    public async Task Post_InvalidPriorityOrDate_Returns400_AndCreatesNothing(string query)
    {
        var title = $"invalid-{Guid.NewGuid():N}";

        var response = await _client.PostAsync($"/api/tasks?title={title}&{query}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var all = await _client.GetFromJsonAsync<List<TaskItem>>("/api/tasks");
        Assert.DoesNotContain(all!, t => t.Title == title);
    }

    [Theory]
    [InlineData("""{"title":"x","isComplete":false,"priority":"urgent"}""")]
    [InlineData("""{"title":"x","isComplete":false,"priority":2}""")]
    [InlineData("""{"title":"x","isComplete":false,"dueDate":"2026-02-30"}""")]
    [InlineData("""{"title":"x","isComplete":false,"dueDate":"tomorrow"}""")]
    public async Task Put_InvalidPriorityOrDate_Returns400_AndChangesNothing(string json)
    {
        var created = await CreateAsync("keep me", "Low", Date(2));

        var response = await PutJson(created.Id, json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var task = await _client.GetFromJsonAsync<TaskItem>($"/api/tasks/{created.Id}");
        Assert.Equal("keep me", task!.Title);
        Assert.Equal(TaskPriority.Low, task.Priority);
        Assert.Equal(DateOnly.Parse(Date(2), System.Globalization.CultureInfo.InvariantCulture), task.DueDate);
    }

    [Fact]
    public async Task Put_OmittingPriorityAndDueDate_LeavesThemUnchanged()
    {
        var created = await CreateAsync("legacy client", "High", Date(4));

        var response = await PutJson(created.Id, """{"title":"renamed","isComplete":true}""");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var task = await _client.GetFromJsonAsync<TaskItem>($"/api/tasks/{created.Id}");
        Assert.Equal("renamed", task!.Title);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(DateOnly.Parse(Date(4), System.Globalization.CultureInfo.InvariantCulture), task.DueDate);
    }

    [Fact]
    public async Task Put_ExplicitNullDueDate_ClearsIt_AndPriorityIsKept()
    {
        var created = await CreateAsync("clear date", "High", Date(4));

        var response = await PutJson(created.Id, """{"title":"clear date","isComplete":false,"dueDate":null}""");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var task = await _client.GetFromJsonAsync<TaskItem>($"/api/tasks/{created.Id}");
        Assert.Null(task!.DueDate);
        Assert.Equal(TaskPriority.High, task.Priority);
    }

    [Fact]
    public async Task Put_SetsPriorityAndDueDate()
    {
        var created = await CreateAsync("set both");

        var response = await PutJson(created.Id, $$"""{"title":"set both","isComplete":false,"priority":"low","dueDate":"{{Date(6)}}"}""");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var task = await _client.GetFromJsonAsync<TaskItem>($"/api/tasks/{created.Id}");
        Assert.Equal(TaskPriority.Low, task!.Priority);
        Assert.Equal(DateOnly.Parse(Date(6), System.Globalization.CultureInfo.InvariantCulture), task.DueDate);
    }

    // The list endpoints share one in-memory DB per class, so these tests use a marker in every title and
    // only look at their own tasks.
    private async Task<List<TaskItem>> QueryAsync(string marker, string query)
    {
        var all = await _client.GetFromJsonAsync<List<TaskItem>>($"/api/tasks{query}");
        return all!.Where(t => t.Title.Contains(marker, StringComparison.Ordinal)).ToList();
    }

    [Fact]
    public async Task Get_SortPriority_ReturnsUrgencyOrder_AndDefaultIsIdOrder()
    {
        var m = $"sort{Guid.NewGuid():N}";
        await CreateAsync($"{m} low", "low");
        await CreateAsync($"{m} high", "high");
        await CreateAsync($"{m} normal");

        var urgency = await QueryAsync(m, "?sort=priority");
        var byId = await QueryAsync(m, "");

        Assert.Equal(new[] { $"{m} high", $"{m} normal", $"{m} low" }, urgency.Select(t => t.Title));
        Assert.Equal(new[] { $"{m} low", $"{m} high", $"{m} normal" }, byId.Select(t => t.Title));
    }

    [Fact]
    public async Task Get_Filters_WorkAlone_AndCombined()
    {
        var m = $"filter{Guid.NewGuid():N}";
        await CreateAsync($"{m} high overdue", "high", Date(-2));
        await CreateAsync($"{m} low overdue", "low", Date(-2));
        await CreateAsync($"{m} high future", "high", Date(5));
        await CreateAsync($"{m} undated");

        Assert.Equal(2, (await QueryAsync(m, "?priority=HIGH")).Count);
        Assert.Equal(2, (await QueryAsync(m, $"?dueBefore={Date(-2)}")).Count);
        Assert.Equal(3, (await QueryAsync(m, $"?dueBefore={Date(5)}")).Count);
        Assert.Equal(2, (await QueryAsync(m, "?overdue=true")).Count);
        Assert.Equal($"{m} high overdue",
            Assert.Single(await QueryAsync(m, $"?priority=high&overdue=true&dueBefore={Date(0)}")).Title);
    }

    [Fact]
    public async Task Get_FilterWithNoMatches_Returns200AndEmptyList()
    {
        var response = await _client.GetAsync($"/api/tasks?priority=high&dueBefore=1999-01-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<TaskItem>>())!);
    }

    [Theory]
    [InlineData("priority=urgent")]
    [InlineData("dueBefore=2026-02-30")]
    [InlineData("overdue=maybe")]
    [InlineData("sort=alphabetical")]
    public async Task Get_InvalidQueryValue_Returns400(string query)
    {
        var response = await _client.GetAsync($"/api/tasks?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

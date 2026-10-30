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

    private async Task<TaskItem> CreateAsync(string title)
    {
        var response = await _client.PostAsync($"/api/tasks?title={Uri.EscapeDataString(title)}", null);
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
}

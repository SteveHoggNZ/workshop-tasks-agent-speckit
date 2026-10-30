using CRUDTasksWithAgent.Models;
using Microsoft.EntityFrameworkCore;

namespace CRUDTasksWithAgent.Tests.Helpers;

public static class TestDb
{
    public static DbContextOptions<TasksDbContext> UniqueOptions() =>
        new DbContextOptionsBuilder<TasksDbContext>()
            .UseInMemoryDatabase($"tests-{Guid.NewGuid():N}")
            .Options;

    public static TasksDbContext Create() => new(UniqueOptions());
}

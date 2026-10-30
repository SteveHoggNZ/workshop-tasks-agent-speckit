using CRUDTasksWithAgent.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CRUDTasksWithAgent.Tests.Helpers;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"api-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TasksDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TasksDbContext>>();
            services.AddDbContext<TasksDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

using Microsoft.EntityFrameworkCore;

namespace CRUDTasksWithAgent.Models
{
    public class TasksDbContext : DbContext
    {
        public TasksDbContext(DbContextOptions<TasksDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; } = null!;
    }
}

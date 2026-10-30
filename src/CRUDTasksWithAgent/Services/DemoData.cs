using CRUDTasksWithAgent.Models;

namespace CRUDTasksWithAgent.Services
{
    public static class DemoData
    {
        // Seeds sample tasks for local workshop use; only runs in Development and only when empty.
        public static void SeedDemoData(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                return;
            }

            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TasksDbContext>();

            if (db.Tasks.Any())
            {
                return;
            }

            db.Tasks.AddRange(
                new TaskItem { Title = "Book the workshop room", IsComplete = true },
                new TaskItem { Title = "Review the Q3 roadmap", IsComplete = false },
                new TaskItem { Title = "Prepare demo for the team", IsComplete = false },
                new TaskItem { Title = "Send meeting notes to the team", IsComplete = true },
                new TaskItem { Title = "Organize the team lunch", IsComplete = false });
            db.SaveChanges();
        }
    }
}

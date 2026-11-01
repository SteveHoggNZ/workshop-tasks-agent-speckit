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

            var today = DateOnly.FromDateTime(DateTime.Now);

            db.Tasks.AddRange(
                new TaskItem { Title = "Book the workshop room", IsComplete = true, DueDate = today.AddDays(-5) },
                new TaskItem { Title = "Review the Q3 roadmap", Priority = TaskPriority.High, DueDate = today.AddDays(-2) },
                new TaskItem { Title = "Prepare demo for the team", Priority = TaskPriority.High, DueDate = today },
                new TaskItem { Title = "Send meeting notes to the team", IsComplete = true },
                new TaskItem { Title = "Organize the team lunch", Priority = TaskPriority.Low, DueDate = today.AddDays(7) },
                new TaskItem { Title = "Update the onboarding doc" });
            db.SaveChanges();
        }
    }
}

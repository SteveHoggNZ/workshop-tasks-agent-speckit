using CRUDTasksWithAgent.Models;
using Microsoft.EntityFrameworkCore;

namespace CRUDTasksWithAgent.Services
{
    // TaskService is used by:
    // 1. The CRUD Home.razor component for UI task management.
    // 2. The mapped API endpoints in Program.cs for backend task operations.
    // 3. The Microsoft Agent Framework tool in TaskCrudTool.cs for agent-based task management.

    public class TaskService
    {
        private readonly TasksDbContext _db;
        private readonly TimeProvider _clock;

        public TaskService(TasksDbContext db, TimeProvider? clock = null)
        {
            _db = db;
            _clock = clock ?? TimeProvider.System;
        }

        public async Task<List<TaskItem>> GetAllTasksAsync() => await _db.Tasks.OrderBy(t => t.Id).ToListAsync();

        public DateOnly Today => DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);

        public bool IsOverdue(TaskItem task) => !task.IsComplete && task.DueDate is { } due && due < Today;

        // Filters run in the store; ordering is applied in memory.
        public async Task<List<TaskItem>> GetTasksAsync(TaskQuery? query = null)
        {
            query ??= new TaskQuery();
            var tasks = _db.Tasks.AsQueryable();
            if (query.Priority is { } priority) tasks = tasks.Where(t => t.Priority == priority);
            if (query.DueOnOrBefore is { } dueBy) tasks = tasks.Where(t => t.DueDate != null && t.DueDate <= dueBy);
            if (query.OverdueOnly)
            {
                var today = Today;
                tasks = tasks.Where(t => !t.IsComplete && t.DueDate != null && t.DueDate < today);
            }

            var list = await tasks.ToListAsync();
            if (!query.UrgencyOrder) return list.OrderBy(t => t.Id).ToList();

            return list
                .OrderBy(t => t.IsComplete)
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate is null)
                .ThenBy(t => t.DueDate)
                .ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Id)
                .ToList();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id) => await _db.Tasks.FindAsync(id);

        public async Task<TaskItem> AddTaskAsync(string title, TaskPriority priority = TaskPriority.Normal, DateOnly? dueDate = null)
        {
            var task = new TaskItem { Title = title, Priority = priority, DueDate = dueDate };
            await _db.Tasks.AddAsync(task);
            await _db.SaveChangesAsync();
            return task;
        }

        public async Task DeleteTaskAsync(TaskItem task)
        {
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateTaskAsync(TaskItem task)
        {
            _db.Tasks.Update(task);
            await _db.SaveChangesAsync();
        }

        public async Task SetTaskCompletionAsync(TaskItem task, bool isComplete)
        {
            task.IsComplete = isComplete;
            _db.Tasks.Update(task);
            await _db.SaveChangesAsync();
        }
    }
}

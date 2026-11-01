using CRUDTasksWithAgent.Models;
using CRUDTasksWithAgent.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.ComponentModel;

namespace CRUDTasksWithAgent.Tools
{
    public class TaskCrudTool
    {
        private readonly TaskService _taskService;

        public TaskCrudTool(TaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>
        /// Creates a new task with a title, completion status, optional priority and optional due date.
        /// </summary>
        [Description("Creates a new task with a title, completion status, optional priority (Low, Normal or High) and optional due date (yyyy-MM-dd).")]
        public async Task<string> CreateTaskAsync(
            [Description("Title of the task")] string title,
            [Description("Whether the task is complete")] bool isComplete = false,
            [Description("Priority: Low, Normal or High (optional, default Normal)")] string? priority = null,
            [Description("Due date in yyyy-MM-dd format (optional)")] string? dueDate = null)
        {
            var taskPriority = TaskPriority.Normal;
            DateOnly? due = null;
            if (!string.IsNullOrWhiteSpace(priority))
            {
                if (!TaskInputParser.TryParsePriority(priority, out taskPriority))
                    return TaskInputParser.PriorityError;
            }
            if (!string.IsNullOrWhiteSpace(dueDate))
            {
                if (!TaskInputParser.TryParseDueDate(dueDate, out var parsed))
                    return TaskInputParser.DueDateError;
                due = parsed;
            }

            var created = await _taskService.AddTaskAsync(title, taskPriority, due);
            if (isComplete) await _taskService.SetTaskCompletionAsync(created, true);
            return $"Task created: {title}";
        }

        /// <summary>
        /// Reads all tasks (most urgent first), only overdue tasks, or a single task if an id is provided.
        /// </summary>
        [Description("Reads tasks, most urgent first, or a single task if an id is provided. Set overdueOnly to list only incomplete tasks whose due date has passed.")]
        public async Task<List<TaskItem>> ReadTasksAsync(
            [Description("Id of the task to read (optional)")] string? id = null,
            [Description("Only return overdue tasks (optional)")] bool overdueOnly = false)
        {
            if (!string.IsNullOrWhiteSpace(id) && int.TryParse(id, out int taskId))
            {
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null) return new List<TaskItem>();
                return new List<TaskItem> { task };
            }
            return await _taskService.GetTasksAsync(new TaskQuery(OverdueOnly: overdueOnly));
        }

        /// <summary>
        /// Updates the specified task fields by id.
        /// </summary>
        [Description("Updates the specified task fields by id, including priority and due date.")]
        public async Task<string> UpdateTaskAsync(
            [Description("Id of the task to update")] string id,
            [Description("New title (optional)")] string? title = null,
            [Description("New completion status (optional)")] bool? isComplete = null,
            [Description("New priority: Low, Normal or High (optional)")] string? priority = null,
            [Description("New due date in yyyy-MM-dd format (optional)")] string? dueDate = null,
            [Description("Set true to remove the due date (optional)")] bool? clearDueDate = null)
        {
            if (!int.TryParse(id, out int taskId))
                return "Invalid task id.";
            var task = await _taskService.GetTaskByIdAsync(taskId);
            if (task == null) return $"Task with Id {taskId} not found.";

            TaskPriority? newPriority = null;
            if (!string.IsNullOrWhiteSpace(priority))
            {
                if (!TaskInputParser.TryParsePriority(priority, out var parsedPriority))
                    return TaskInputParser.PriorityError;
                newPriority = parsedPriority;
            }
            DateOnly? newDueDate = null;
            if (!string.IsNullOrWhiteSpace(dueDate))
            {
                if (clearDueDate == true)
                    return "Use either dueDate or clearDueDate, not both.";
                if (!TaskInputParser.TryParseDueDate(dueDate, out var parsedDate))
                    return TaskInputParser.DueDateError;
                newDueDate = parsedDate;
            }

            if (!string.IsNullOrWhiteSpace(title)) task.Title = title;
            if (isComplete.HasValue) task.IsComplete = isComplete.Value;
            if (newPriority.HasValue) task.Priority = newPriority.Value;
            if (newDueDate.HasValue) task.DueDate = newDueDate;
            if (clearDueDate == true) task.DueDate = null;
            await _taskService.UpdateTaskAsync(task);
            return $"Task {task.Id} updated.";
        }

        /// <summary>
        /// Deletes a task by id.
        /// </summary>
        /// <param name="id">Id of the task to delete</param>
        [Description("Deletes a task by id.")]
        public async Task<string> DeleteTaskAsync(
            [Description("Id of the task to delete")] string id)
        {
            if (!int.TryParse(id, out int taskId))
                return "Invalid task id.";
            var task = await _taskService.GetTaskByIdAsync(taskId);
            if (task == null) return $"Task with Id {taskId} not found.";
            await _taskService.DeleteTaskAsync(task);
            return $"Task {taskId} deleted.";
        }

        private static string FormatTask(TaskItem t) =>
            $"Id: {t.Id}\nTitle: {t.Title}\nComplete: {(t.IsComplete ? "Yes" : "No")}";
    }
}
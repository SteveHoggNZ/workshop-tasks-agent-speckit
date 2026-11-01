using System.Globalization;
using CRUDTasksWithAgent.Models;

namespace CRUDTasksWithAgent.Services;

public static class TaskInputParser
{
    public const string PriorityError = "Priority must be one of: Low, Normal, High.";
    public const string DueDateError = "Due date must be a real calendar date in the format yyyy-MM-dd.";

    public static bool TryParsePriority(string? text, out TaskPriority priority) =>
        TaskPriorityExtensions.TryParse(text, out priority);

    public static bool TryParseDueDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}

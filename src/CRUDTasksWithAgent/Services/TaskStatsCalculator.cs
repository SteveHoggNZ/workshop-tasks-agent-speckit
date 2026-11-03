using CRUDTasksWithAgent.Models;

namespace CRUDTasksWithAgent.Services;

public class TaskStatsCalculator
{
    public double CompletionRate(IReadOnlyList<TaskItem> tasks)
    {
        double completed = tasks.Count(t => t.IsComplete);
        double total = tasks.Count;
        return completed * 100 / total;
    }

    public DateTime WeekStart(DateTimeOffset now)
    {
        var today = now.LocalDateTime.Date;
        return today.AddDays((int)DayOfWeek.Monday - (int)today.DayOfWeek);
    }

    public string WeekLabel(DateTimeOffset now) => WeekStart(now).ToString("ddd d MMM yyyy");
}

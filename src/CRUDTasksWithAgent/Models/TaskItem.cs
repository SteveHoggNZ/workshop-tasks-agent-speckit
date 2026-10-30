namespace CRUDTasksWithAgent.Models
{
    public class TaskItem
    {
        public int Id { get; set; } // Primary key for EF Core
        public string Title { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
    }
}

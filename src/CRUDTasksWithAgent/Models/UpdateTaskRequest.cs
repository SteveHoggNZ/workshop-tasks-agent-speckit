using System.Text.Json.Serialization;

namespace CRUDTasksWithAgent.Models;

// Body of PUT /api/tasks/{id}. The DueDate setter only runs when the property is present in the JSON,
// so an omitted due date (unchanged) can be told apart from an explicit null (clear).
public class UpdateTaskRequest
{
    private DateOnly? _dueDate;

    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public TaskPriority? Priority { get; set; }

    public DateOnly? DueDate
    {
        get => _dueDate;
        set
        {
            _dueDate = value;
            DueDateSpecified = true;
        }
    }

    [JsonIgnore]
    public bool DueDateSpecified { get; private set; }
}

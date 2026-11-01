using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRUDTasksWithAgent.Models;

[JsonConverter(typeof(TaskPriorityJsonConverter))]
public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}

public static class TaskPriorityExtensions
{
    // Accepts only the names Low, Normal and High in any letter case; numbers and unknown names are rejected.
    public static bool TryParse(string? text, out TaskPriority priority)
    {
        priority = TaskPriority.Normal;
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !char.IsLetter(trimmed[0])) return false;
        return Enum.TryParse(trimmed, ignoreCase: true, out priority) && Enum.IsDefined(priority);
    }
}

public sealed class TaskPriorityJsonConverter : JsonConverter<TaskPriority>
{
    public override TaskPriority Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && TaskPriorityExtensions.TryParse(reader.GetString(), out var priority))
            return priority;
        throw new JsonException("Priority must be one of: Low, Normal, High.");
    }

    public override void Write(Utf8JsonWriter writer, TaskPriority value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

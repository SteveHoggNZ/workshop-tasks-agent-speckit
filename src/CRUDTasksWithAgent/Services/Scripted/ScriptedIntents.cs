using System.Text.Json;
using System.Text.RegularExpressions;

namespace CRUDTasksWithAgent.Services.Scripted
{
    // The single table of commands understood by the scripted model.
    // To teach it a new command, add a row to All.
    public static class ScriptedIntents
    {
        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        public static IReadOnlyList<ScriptedIntent> All { get; } = new List<ScriptedIntent>
        {
            new(
                Name: "add",
                Pattern: new Regex(@"^\s*(?:add|create)\s+(?:a\s+)?task\s*:?\s+(?<title>.+?)(?:\s+priority\s+(?<priority>\w+))?(?:\s+due\s+(?<due>\S+))?\s*$", Options),
                ToolName: "CreateTask",
                Arguments: m =>
                {
                    var args = new Dictionary<string, object?> { ["title"] = m.Groups["title"].Value };
                    if (m.Groups["priority"].Success) args["priority"] = m.Groups["priority"].Value;
                    if (m.Groups["due"].Success) args["dueDate"] = m.Groups["due"].Value;
                    return args;
                },
                Usage: "add task <title> [priority low|normal|high] [due yyyy-mm-dd]",
                Example: "add task Send report priority high due 2026-12-01"),

            new(
                Name: "list",
                Pattern: new Regex(@"^\s*(?:list|show)(?:\s+(?:all|my))?\s+tasks?\s*$", Options),
                ToolName: "ReadTasks",
                Arguments: _ => new Dictionary<string, object?>(),
                Usage: "list tasks",
                Example: "list tasks",
                Summarize: (_, result) => FormatTaskList(result, "You have no tasks.")),

            new(
                Name: "overdue",
                Pattern: new Regex(@"^\s*(?:list|show)\s+overdue(?:\s+tasks?)?\s*$", Options),
                ToolName: "ReadTasks",
                Arguments: _ => new Dictionary<string, object?> { ["overdueOnly"] = true },
                Usage: "show overdue",
                Example: "show overdue",
                Summarize: (_, result) => FormatTaskList(result, "Nothing is overdue.")),

            new(
                Name: "priority",
                Pattern: new Regex(@"^\s*priority\s+(?:task\s+)?#?(?<id>\d+)\s+(?<level>\S+)\s*$", Options),
                ToolName: "UpdateTask",
                Arguments: m => new Dictionary<string, object?> { ["id"] = m.Groups["id"].Value, ["priority"] = m.Groups["level"].Value },
                Usage: "priority <id> low|normal|high",
                Example: "priority 3 high"),

            new(
                Name: "due",
                Pattern: new Regex(@"^\s*due\s+(?:task\s+)?#?(?<id>\d+)\s+(?<date>\S+)\s*$", Options),
                ToolName: "UpdateTask",
                Arguments: m => new Dictionary<string, object?> { ["id"] = m.Groups["id"].Value, ["dueDate"] = m.Groups["date"].Value },
                Usage: "due <id> yyyy-mm-dd",
                Example: "due 3 2026-12-01"),

            new(
                Name: "clear-due",
                Pattern: new Regex(@"^\s*clear\s+due(?:\s+date)?\s+(?:task\s+)?#?(?<id>\d+)\s*$", Options),
                ToolName: "UpdateTask",
                Arguments: m => new Dictionary<string, object?> { ["id"] = m.Groups["id"].Value, ["clearDueDate"] = true },
                Usage: "clear due <id>",
                Example: "clear due 3"),

            new(
                Name: "complete",
                Pattern: new Regex(@"^\s*(?:complete|done)\s+(?:task\s+)?#?(\d)", Options),
                ToolName: "UpdateTask",
                Arguments: m => new Dictionary<string, object?> { ["id"] = m.Groups[1].Value, ["isComplete"] = true },
                Usage: "complete <id>",
                Example: "complete 2"),

            new(
                Name: "rename",
                Pattern: new Regex(@"^\s*rename\s+(?:task\s+)?#?(\d)\S*\s+to\s+(?<title>.+?)\s*$", Options),
                ToolName: "UpdateTask",
                Arguments: m => new Dictionary<string, object?> { ["id"] = m.Groups[1].Value, ["title"] = m.Groups["title"].Value },
                Usage: "rename <id> to <title>",
                Example: "rename 3 to Prepare the Spec Kit demo"),

            new(
                Name: "delete",
                Pattern: new Regex(@"^\s*(?:delete|remove)\s+(?:task\s+)?#?(\d)", Options),
                ToolName: "DeleteTask",
                Arguments: m => new Dictionary<string, object?> { ["id"] = m.Groups[1].Value },
                Usage: "delete <id>",
                Example: "delete 5"),
        };

        // Suggested prompts for the chat page: one working example per command, plus "help",
        // which any unrecognised message also returns.
        public static IReadOnlyList<string> Suggestions { get; } =
            All.Select(i => i.Example).Append("help").ToList();

        public static string HelpText { get; } =
            "I'm the scripted offline model. I understand these commands:\n" +
            string.Join("\n", All.Select(i => $"- {i.Usage}"));

        private static string FormatTaskList(object? result, string emptyMessage)
        {
            if (result is null) return emptyMessage;

            var json = JsonSerializer.SerializeToElement(result);
            if (json.ValueKind == JsonValueKind.String) return json.GetString() ?? string.Empty;
            if (json.ValueKind != JsonValueKind.Array || json.GetArrayLength() == 0) return emptyMessage;

            var lines = json.EnumerateArray().Select(t =>
            {
                var id = Prop(t, "id");
                var title = Prop(t, "title");
                var done = t.TryGetProperty("isComplete", out var c) || t.TryGetProperty("IsComplete", out c)
                    ? c.ValueKind == JsonValueKind.True : false;
                var priority = Prop(t, "priority");
                var due = Prop(t, "dueDate");
                var details = string.IsNullOrEmpty(due) ? priority : $"{priority}, due {due}";
                return $"{id}. [{(done ? "x" : " ")}] {title} ({details})";
            });
            return "Here are your tasks:\n" + string.Join("\n", lines);
        }

        private static string Prop(JsonElement e, string camelName)
        {
            if (e.TryGetProperty(camelName, out var v) || e.TryGetProperty(char.ToUpperInvariant(camelName[0]) + camelName[1..], out v))
                return v.ToString();
            return string.Empty;
        }
    }
}

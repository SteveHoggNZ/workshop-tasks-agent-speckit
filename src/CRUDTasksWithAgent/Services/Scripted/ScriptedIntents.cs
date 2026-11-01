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
                Pattern: new Regex(@"^\s*(?:add|create)\s+(?:a\s+)?task\s*:?\s+(?<title>.+?)\s*$", Options),
                ToolName: "CreateTask",
                Arguments: m => new Dictionary<string, object?> { ["title"] = m.Groups["title"].Value },
                Usage: "add task <title>",
                Example: "add task Buy milk"),

            new(
                Name: "list",
                Pattern: new Regex(@"^\s*(?:list|show)(?:\s+(?:all|my))?\s+tasks?\s*$", Options),
                ToolName: "ReadTasks",
                Arguments: _ => new Dictionary<string, object?>(),
                Usage: "list tasks",
                Example: "list tasks",
                Summarize: (_, result) => FormatTaskList(result)),

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

        private static string FormatTaskList(object? result)
        {
            if (result is null) return "You have no tasks.";

            var json = JsonSerializer.SerializeToElement(result);
            if (json.ValueKind == JsonValueKind.String) return json.GetString() ?? string.Empty;
            if (json.ValueKind != JsonValueKind.Array || json.GetArrayLength() == 0) return "You have no tasks.";

            var lines = json.EnumerateArray().Select(t =>
            {
                var id = Prop(t, "id");
                var title = Prop(t, "title");
                var done = t.TryGetProperty("isComplete", out var c) || t.TryGetProperty("IsComplete", out c)
                    ? c.ValueKind == JsonValueKind.True : false;
                return $"{id}. [{(done ? "x" : " ")}] {title}";
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

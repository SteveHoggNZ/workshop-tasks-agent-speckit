using System.Text.RegularExpressions;

namespace CRUDTasksWithAgent.Services.Scripted
{
    // One row of the scripted model's grammar: a pattern on the user's message that maps to a tool call.
    // Arguments builds the tool arguments (keyed by parameter name) from the regex match.
    // Summarize turns the tool result into the final reply; when null the raw result text is used.
    // Usage is the line shown in the help text.
    public sealed record ScriptedIntent(
        string Name,
        Regex Pattern,
        string ToolName,
        Func<Match, IDictionary<string, object?>> Arguments,
        string Usage,
        Func<Match, object?, string>? Summarize = null);
}

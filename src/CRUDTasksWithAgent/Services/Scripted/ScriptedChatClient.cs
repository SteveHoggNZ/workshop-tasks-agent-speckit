using Microsoft.Extensions.AI;

namespace CRUDTasksWithAgent.Services.Scripted
{
    // A deterministic, offline IChatClient. It never calls a model: it matches the last user message
    // against the intent table and answers with a real FunctionCallContent, so the Agent Framework
    // function-invocation pipeline runs the real tools. When the tool result comes back it replies
    // with a short summary.
    public sealed class ScriptedChatClient : IChatClient
    {
        private readonly IReadOnlyList<ScriptedIntent> _intents;

        public ScriptedChatClient() : this(ScriptedIntents.All) { }

        public ScriptedChatClient(IReadOnlyList<ScriptedIntent> intents)
        {
            _intents = intents;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var history = messages.ToList();
            var userText = history.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
            var (intent, match) = Match(userText);

            var toolResult = history.LastOrDefault()?.Contents.OfType<FunctionResultContent>().FirstOrDefault();
            if (toolResult is not null)
            {
                var text = intent?.Summarize is { } summarize
                    ? summarize(match!, toolResult.Result)
                    : toolResult.Result?.ToString() ?? "Done.";
                return Task.FromResult(Reply(new TextContent(text)));
            }

            if (intent is null)
            {
                return Task.FromResult(Reply(new TextContent(ScriptedIntents.HelpText)));
            }

            var call = new FunctionCallContent(Guid.NewGuid().ToString("N"), intent.ToolName, intent.Arguments(match!));
            return Task.FromResult(Reply(call));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await GetResponseAsync(messages, options, cancellationToken);
            foreach (var update in response.ToChatResponseUpdates())
            {
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }

        private (ScriptedIntent? Intent, System.Text.RegularExpressions.Match? Match) Match(string text)
        {
            foreach (var intent in _intents)
            {
                var m = intent.Pattern.Match(text);
                if (m.Success) return (intent, m);
            }
            return (null, null);
        }

        private static ChatResponse Reply(AIContent content) =>
            new(new ChatMessage(ChatRole.Assistant, [content]));
    }
}

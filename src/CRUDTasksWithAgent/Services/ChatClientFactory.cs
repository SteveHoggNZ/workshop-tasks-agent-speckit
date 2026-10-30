using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using CRUDTasksWithAgent.Services.Scripted;
using Microsoft.Extensions.AI;
using OpenAI;

namespace CRUDTasksWithAgent.Services
{
    public interface IChatClientFactory
    {
        // Creates the IChatClient for the configured Llm:Provider. Throws if the provider is unknown or misconfigured.
        IChatClient Create();
    }

    public class ChatClientFactory : IChatClientFactory
    {
        private const string GitHubModelsEndpoint = "https://models.github.ai/inference";
        private readonly IConfiguration _config;

        public ChatClientFactory(IConfiguration config)
        {
            _config = config;
        }

        public IChatClient Create()
        {
            var provider = _config["Llm:Provider"];
            if (string.IsNullOrWhiteSpace(provider)) provider = "Scripted";

            return provider.Trim().ToLowerInvariant() switch
            {
                "scripted" => new ScriptedChatClient(),
                "githubmodels" => CreateOpenAI(
                    new Uri(GitHubModelsEndpoint),
                    Required(_config["Llm:ApiKey"] ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN"), "Llm:ApiKey or GITHUB_TOKEN"),
                    Or(_config["Llm:Model"], "openai/gpt-4.1-mini")),
                "ollama" => CreateOpenAI(
                    new Uri(Or(_config["Llm:Endpoint"], "http://localhost:11434/v1")),
                    "ollama",
                    Or(_config["Llm:Model"], "qwen2.5:7b")),
                "openai" => CreateOpenAI(
                    null,
                    Required(_config["Llm:ApiKey"], "Llm:ApiKey"),
                    Required(_config["Llm:Model"], "Llm:Model")),
                "azureopenai" => CreateAzureOpenAI(),
                _ => throw new InvalidOperationException($"Unknown Llm:Provider '{provider}'. Use Scripted, GitHubModels, Ollama, OpenAI or AzureOpenAI.")
            };
        }

        private static IChatClient CreateOpenAI(Uri? endpoint, string apiKey, string model)
        {
            var options = new OpenAIClientOptions();
            if (endpoint is not null) options.Endpoint = endpoint;
            return new OpenAIClient(new ApiKeyCredential(apiKey), options)
                .GetChatClient(model)
                .AsIChatClient();
        }

        private IChatClient CreateAzureOpenAI()
        {
            var deployment = Required(_config["ModelDeployment"], "ModelDeployment");
            var endpoint = Required(_config["AzureOpenAIEndpoint"], "AzureOpenAIEndpoint");

            // An API key avoids az login / Entra ID; without it, fall back to DefaultAzureCredential
            var apiKey = _config["AzureOpenAIApiKey"];
            var client = string.IsNullOrWhiteSpace(apiKey)
                ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
                : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

            return client.GetChatClient(deployment).AsIChatClient();
        }

        private static string Or(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;

        private static string Required(string? value, string name) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException($"Missing required setting: {name}.")
                : value;
    }
}

using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using System.Net.Http.Json;

namespace EnterpriseRag.Infrastructure.AI
{
    public class OllamaChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        public OllamaChatService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<RagAnswer> GenerateAsync(RagContext context, CancellationToken cancellationToken)
        {
            var prompt = BuildPrompt(context);
            var request = new
            {
                model = "qwen3:8b",
                stream = false,
                messages = new[]
                 {
                    new
                    {
                        role = "system",
                        content =
                            "You are a helpful assistant. " +
                            "Answer questions using only the provided context. " +
                            "If the context does not contain enough information, " +
                            "say that you do not have enough information."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
            };


            var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
            if (result is null || result.Message is null)
            {
                throw new InvalidOperationException(
                    "Ollama returned an empty chat response.");
            }

            return new RagAnswer
            {
                Answer = result.Message.Content,
                Sources = context.Results
            };
        }

        private static string BuildPrompt(RagContext context)
        {
            var contextText = string.Join(
                "\n\n",
                context.Results.Select((result, index) =>
                    $"""
                [Source {index + 1} - Page {result.PageNumber}]
                {result.Content}
                """));

            return $"""
                Use the following context to answer the question.

                Context:
                {contextText}

                Question:
                {context.Question}
                """;
        }

        private sealed class OllamaChatResponse
        {
            public OllamaMessage? Message { get; set; }
        }

        private sealed class OllamaMessage
        {
            public string Content { get; set; } = string.Empty;
        }
    }
}

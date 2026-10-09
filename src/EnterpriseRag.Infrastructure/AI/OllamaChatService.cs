using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Application.Rag;
using System.Net.Http.Json;
using System.Text.Json;

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
            var responseSchema = new
            {
                type = "object",
                properties = new
                {
                    answer = new
                    {
                        type = "string"
                    },
                    citationIds = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "integer"
                        }
                    }
                },
                required = new[]
                {
                    "answer",
                    "citationIds"
                }
            };

            var request = new
            {
                model = "qwen3:8b",
                think = false,
                stream = false,
                format = responseSchema,
                messages = new[]
                 {
                    new
                    {
                        role = "system",
                        content =
                            "You are a helpful assistant. " +
                            "Answer questions using only the provided context. " +
                            "If the context does not contain enough information, " +
                            "say that you do not have enough information." +
                            "Do not mention context numbers, context IDs, or the retrieval process in your answer." +
                            "Return only the answer to the user's question." +
                            "Use citationIds only to identify which context items support your answer."
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

            var ollamaResult = JsonSerializer.Deserialize<OllamaRagResponse>(result.Message.Content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (ollamaResult is null)
            {
                throw new InvalidOperationException(
                    "Ollama returned an invalid RAG response.");
            }

            var citations = RagCitationMapper.Map(ollamaResult.CitationIds, context.Results);
            return new RagAnswer
            {
                Answer = ollamaResult.Answer,
                Citations = citations
            };
        }

        private static string BuildPrompt(RagContext context)
        {
            var contextText = string.Join(
                "\n\n",
                context.Results.Select((result, index) =>
                    $"""
                    [Context {index + 1}]
                    File: {result.FileName}
                    Page: {result.PageNumber}
                    Content:
                    {result.Content}
                    """));

            return $"""
            Use the following context to answer the question.

            Each context item has a number.
            If you use information from a context item,
            include its number in the citationIds array.

            Only use citation IDs that exist in the provided context.

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

        private sealed class OllamaRagResponse
        {
            public string Answer { get; set; } = string.Empty;
            public List<int> CitationIds { get; set; } = [];
        }
    }
}

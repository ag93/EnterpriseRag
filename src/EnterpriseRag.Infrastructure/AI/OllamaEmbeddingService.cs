using EnterpriseRag.Application.Abstractions;
using System.Net.Http.Json;

namespace EnterpriseRag.Infrastructure.AI
{
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        public OllamaEmbeddingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string texts, CancellationToken cancellationToken)
        {
            var requestBody = new
            {
                model = "nomic-embed-text",
                input = texts
            };

            var response = await _httpClient.PostAsJsonAsync(
            "/api/embed",
            requestBody,
            cancellationToken);

            response.EnsureSuccessStatusCode();
            var result =
            await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken);
            
            if (result is null || result.Embeddings.Count == 0)
            {
                throw new InvalidOperationException(
                    "Ollama returned no embeddings.");
            }

            return result.Embeddings[0];
        }

        private sealed class OllamaEmbeddingResponse
        {
            public List<float[]> Embeddings { get; set; } = [];
        }
    }
}

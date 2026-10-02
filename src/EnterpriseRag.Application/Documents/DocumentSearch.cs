using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Documents
{
    public class DocumentSearch : IDocumentSearch
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;

        public DocumentSearch(IEmbeddingService embeddingService, IVectorStore vectorStore)
        {
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
        }

        public async Task<IReadOnlyList<SearchResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                throw new ArgumentException(
                    "Search query is required.",
                    nameof(request));
            }

            if (request.TopK <= 0)
            {
                throw new ArgumentException(
                    "TopK must be greater than zero.",
                    nameof(request));
            }

            var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
            var vectorResults = await _vectorStore.SearchAsync(embedding, request.TopK, cancellationToken);

            return vectorResults
            .Select(result => new SearchResult
            {
                DocumentId = result.DocumentId,
                ChunkId = result.ChunkId,
                Content = result.Content,
                PageNumber = result.PageNumber,
                Score = result.Score
            }).ToList();
        }
    }
}

using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Documents
{
    public class DocumentSearch : IDocumentSearch
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;
        private readonly IReranker _reranker;
        private readonly bool _enableReranking;

        public DocumentSearch(IEmbeddingService embeddingService, IVectorStore vectorStore, IReranker reranker, bool enableReranking = true)
        {
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
            _reranker = reranker;
            _enableReranking = enableReranking;
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

            if (request.CandidateCount < request.TopK)
            {
                throw new ArgumentException(
                    "CandidateCount must be greater than or equal to TopK.",
                    nameof(request));
            }

            //Add filter logic here?

            var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
            var candidates = await _vectorStore.SearchAsync(embedding, request.CandidateCount, request.Filter, cancellationToken);

            var searchResults = candidates
                .Select(result => new SearchResult
                {
                    DocumentId = result.DocumentId,
                    ChunkId = result.ChunkId,
                    FileName = result.FileName,
                    Content = result.Content,
                    PageNumber = result.PageNumber,
                    Score = result.Score
                })
                .ToList();

            if (!_enableReranking)
            {
                return searchResults
                    .Take(request.TopK)
                    .ToList();
            }

            var reranked = await _reranker.RerankAsync(request.Query, searchResults, cancellationToken);
            return reranked.Take(request.TopK).ToList();
        }
    }
}

using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Tests.Documents;

public class DocumentSearchTests
{
    [Fact]
    public async Task SearchAsync_ForwardsAuthorizationFilterToVectorStore()
    {
        // Arrange
        var authorizedDocumentId = Guid.NewGuid();

        var filter = new DocumentSearchFilter
        {
            DocumentIds = [authorizedDocumentId]
        };

        var vectorStore = new FakeVectorStore([]);
        var reranker = new FakeReranker();

        var service = new DocumentSearch(
            new FakeEmbeddingService(),
            vectorStore,
            reranker);

        var request = new SearchRequest
        {
            Query = "authorized content",
            TopK = 5,
            CandidateCount = 20,
            Filter = filter
        };

        // Act
        await service.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Same(filter, vectorStore.ReceivedFilter);
        Assert.Equal(
            new[] { authorizedDocumentId },
            vectorStore.ReceivedFilter!.DocumentIds);
    }

    [Fact]
    public async Task SearchAsync_EmptyAuthorizationFilter_ForwardsEmptyFilter()
    {
        // Arrange
        var filter = new DocumentSearchFilter
        {
            DocumentIds = Array.Empty<Guid>()
        };

        var vectorStore = new FakeVectorStore([]);
        var reranker = new FakeReranker();

        var service = new DocumentSearch(
            new FakeEmbeddingService(),
            vectorStore,
            reranker);

        var request = new SearchRequest
        {
            Query = "some content",
            TopK = 5,
            CandidateCount = 20,
            Filter = filter
        };

        // Act
        await service.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(vectorStore.ReceivedFilter);
        Assert.NotNull(vectorStore.ReceivedFilter.DocumentIds);
        Assert.Empty(vectorStore.ReceivedFilter.DocumentIds);
    }

    [Fact]
    public async Task SearchAsync_RerankerReceivesOnlyVectorStoreCandidates()
    {
        // Arrange
        var authorizedDocumentId = Guid.NewGuid();

        var candidate = new VectorSearchResult
        {
            DocumentId = authorizedDocumentId,
            ChunkId = Guid.NewGuid(),
            FileName = "authorized.pdf",
            Content = "Authorized content",
            PageNumber = 1,
            Score = 0.9f
        };

        var vectorStore = new FakeVectorStore([candidate]);
        var reranker = new FakeReranker();

        var service = new DocumentSearch(
            new FakeEmbeddingService(),
            vectorStore,
            reranker);

        var request = new SearchRequest
        {
            Query = "authorized content",
            TopK = 5,
            CandidateCount = 20,
            Filter = new DocumentSearchFilter
            {
                DocumentIds = [authorizedDocumentId]
            }
        };

        // Act
        await service.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(reranker.ReceivedResults!);
        Assert.Equal(
            authorizedDocumentId,
            reranker.ReceivedResults![0].DocumentId);
    }


    [Fact]
    public async Task SearchAsync_RerankerNeverReceivesUnauthorizedCandidates()
    {
        // Arrange
        var authorizedDocumentId = Guid.NewGuid();
        var unauthorizedDocumentId = Guid.NewGuid();

        var authorizedCandidate = new VectorSearchResult
        {
            DocumentId = authorizedDocumentId,
            ChunkId = Guid.NewGuid(),
            FileName = "authorized.pdf",
            Content = "Authorized content",
            PageNumber = 1,
            Score = 0.9f
        };

        var unauthorizedCandidate = new VectorSearchResult
        {
            DocumentId = unauthorizedDocumentId,
            ChunkId = Guid.NewGuid(),
            FileName = "confidential.pdf",
            Content = "Confidential content",
            PageNumber = 1,
            Score = 0.95f
        };

        // Deliberately ignores the authorization filter and returns both candidates.
        var vectorStore = new FakeVectorStore([authorizedCandidate, unauthorizedCandidate]);
        var reranker = new FakeReranker();

        var service = new DocumentSearch(new FakeEmbeddingService(), vectorStore, reranker);

        var request = new SearchRequest
        {
            Query = "authorized content",
            TopK = 5,
            CandidateCount = 20,
            Filter = new DocumentSearchFilter
            {
                DocumentIds = [authorizedDocumentId]
            }
        };

        // Act
        var results = await service.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(reranker.ReceivedResults);
        Assert.Single(reranker.ReceivedResults);
        Assert.Equal(
            authorizedDocumentId,
            reranker.ReceivedResults[0].DocumentId);

        Assert.Single(results);
        Assert.Equal(authorizedDocumentId, results[0].DocumentId);
    }

    private sealed class FakeEmbeddingService : IEmbeddingService
    {
        public Task<float[]> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new float[] { 0.1f, 0.2f });
        }
    }

    private sealed class FakeVectorStore : IVectorStore
    {
        private readonly IReadOnlyList<VectorSearchResult> _results;

        public DocumentSearchFilter? ReceivedFilter { get; private set; }

        public FakeVectorStore(IReadOnlyList<VectorSearchResult> results)
        {
            _results = results;
        }

        public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] embedding, int limit, DocumentSearchFilter? filter, CancellationToken cancellationToken)
        {
            ReceivedFilter = filter;
            return Task.FromResult(_results);
        }

        public Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReranker : IReranker
    {
        public IReadOnlyList<SearchResult>? ReceivedResults { get; private set; }

        public Task<IReadOnlyList<SearchResult>> RerankAsync(string query, IReadOnlyList<SearchResult> results, CancellationToken cancellationToken)
        {
            ReceivedResults = results;
            return Task.FromResult(results);
        }
    }
}

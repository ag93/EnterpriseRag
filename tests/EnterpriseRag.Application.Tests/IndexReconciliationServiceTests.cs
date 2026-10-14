using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Infrastructure.Documents;
using EnterpriseRag.Domain;

namespace EnterpriseRag.Application.Tests.Documents;

public class IndexReconciliationServiceTests
{
    [Fact]
    public async Task ReconcileAsync_IdentifiesMissingAndOrphanedChunkIds()
    {
        var existingChunkId = Guid.NewGuid();
        var missingChunkId = Guid.NewGuid();
        var orphanedChunkId = Guid.NewGuid();

        var documentRepository = new FakeDocumentRepository([existingChunkId, missingChunkId]);
        var vectorStore = new FakeVectorStore([existingChunkId, orphanedChunkId]);

        var service = new IndexReconciliationService(documentRepository, vectorStore, 
            new FakeDocumentIndexer(vectorStore, new Dictionary<Guid, IReadOnlyCollection<Guid>>()));

        var result = await service.ReconcileAsync(CancellationToken.None);

        Assert.Equal([missingChunkId], result.MissingInVectorStore);
        Assert.Equal([orphanedChunkId], result.OrphanedInVectorStore);
        Assert.True(result.HadInconsistencies);
    }

    [Fact]
    public async Task ReconcileAsync_WhenIdsMatch_ReturnsNoDifferences()
    {
        var chunkId = Guid.NewGuid();

        var documentRepository = new FakeDocumentRepository([chunkId]);
        var vectorStore = new FakeVectorStore([chunkId]);

        var service = new IndexReconciliationService(documentRepository, vectorStore, new FakeDocumentIndexer(vectorStore,
            new Dictionary<Guid, IReadOnlyCollection<Guid>>()));

        var result = await service.ReconcileAsync(CancellationToken.None);

        Assert.Empty(result.MissingInVectorStore);
        Assert.Empty(result.OrphanedInVectorStore);
        Assert.False(result.HadInconsistencies);
    }

    [Fact]
    public async Task ReconcileAsync_IndexesMissingChunksAndDeletesOrphans()
    {
        var existingChunkId = Guid.NewGuid();
        var missingChunkId = Guid.NewGuid();
        var orphanedChunkId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var documentRepository = new FakeDocumentRepository([existingChunkId, missingChunkId],
            new Dictionary<Guid, Guid>
            {
                [missingChunkId] = documentId
            });

        var vectorStore = new FakeVectorStore([existingChunkId, orphanedChunkId]);

        var indexer = new FakeDocumentIndexer(vectorStore, new Dictionary<Guid, 
            IReadOnlyCollection<Guid>>
            {
                [documentId] = [missingChunkId]
            });

        var service = new IndexReconciliationService(documentRepository, vectorStore, indexer);

        var result = await service.ReconcileAsync(CancellationToken.None);

        Assert.Equal([missingChunkId], result.MissingInVectorStore);
        Assert.Equal([orphanedChunkId], result.OrphanedInVectorStore);
        Assert.Equal([documentId], indexer.IndexedDocumentIds);
        Assert.Equal([orphanedChunkId], vectorStore.DeletedChunkIds);
        Assert.True(result.HadInconsistencies);
    }

    [Fact]
    public async Task ReconcileAsync_WhenIndexingFails_DoesNotDeleteOrphans()
    {
        var missingChunkId = Guid.NewGuid();
        var orphanedChunkId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var documentRepository = new FakeDocumentRepository(
            [missingChunkId],
            new Dictionary<Guid, Guid>
            {
                [missingChunkId] = documentId
            });

        var vectorStore = new FakeVectorStore([orphanedChunkId]);
        var indexer = new FailingDocumentIndexer();

        var service = new IndexReconciliationService(
            documentRepository,
            vectorStore,
            indexer);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ReconcileAsync(CancellationToken.None));

        Assert.Empty(vectorStore.DeletedChunkIds);
    }


    [Fact]
    public async Task ReconcileAsync_WhenRunAgainAfterSuccessfulRepair_FindsNoDifferences()
    {
        var existingChunkId = Guid.NewGuid();
        var missingChunkId = Guid.NewGuid();
        var orphanedChunkId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var documentRepository = new FakeDocumentRepository(
            [existingChunkId, missingChunkId],
            new Dictionary<Guid, Guid>
            {
                [missingChunkId] = documentId
            });

        var vectorStore = new FakeVectorStore(
            [existingChunkId, orphanedChunkId]);

        var indexer = new FakeDocumentIndexer(
            vectorStore,
            new Dictionary<Guid, IReadOnlyCollection<Guid>>
            {
                [documentId] = [existingChunkId, missingChunkId]
            });

        var service = new IndexReconciliationService(
            documentRepository,
            vectorStore,
            indexer);

        var firstResult = await service.ReconcileAsync(
            CancellationToken.None);

        Assert.Equal([missingChunkId], firstResult.MissingInVectorStore);
        Assert.Equal([orphanedChunkId], firstResult.OrphanedInVectorStore);

        var secondResult = await service.ReconcileAsync(
            CancellationToken.None);

        Assert.Empty(secondResult.MissingInVectorStore);
        Assert.Empty(secondResult.OrphanedInVectorStore);
        Assert.False(secondResult.HadInconsistencies);
    }

    private sealed class FailingDocumentIndexer : IDocumentIndexer
    {
        public Task IndexAsync(
            Guid documentId,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated indexing failure.");
        }
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        private readonly IReadOnlyCollection<Guid> _chunkIds;
        private readonly Dictionary<Guid, Guid> _documentIdsByChunk;

        public FakeDocumentRepository(IReadOnlyCollection<Guid> chunkIds, Dictionary<Guid, Guid>? documentIdsByChunk = null)
        {
            _chunkIds = chunkIds;
            _documentIdsByChunk = documentIdsByChunk ?? new Dictionary<Guid, Guid>();
        }

        public Task<IReadOnlyCollection<Guid>> GetAllChunkIdsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_chunkIds);
        }

        public Task<IReadOnlyCollection<Guid>> GetDocumentIdsForChunkIdsAsync(IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken)
        {
            return Task.FromResult(_documentIdsByChunk
                .Where(pair => chunkIds.Contains(pair.Key))
                .Select(pair => pair.Value)
                .Distinct()
                .ToArray() as IReadOnlyCollection<Guid>  ?? Array.Empty<Guid>());
        }

        public Task AddAsync(Document document, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(Guid documentId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeVectorStore : IVectorStore
    {
        private readonly HashSet<Guid> _chunkIds;

        public IReadOnlyCollection<Guid> DeletedChunkIds { get; private set; }
            = Array.Empty<Guid>();

        public FakeVectorStore(IReadOnlyCollection<Guid> chunkIds)
        {
            _chunkIds = chunkIds.ToHashSet();
        }

        public Task<IReadOnlyCollection<Guid>> GetExistingChunkIdsAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Guid>>(_chunkIds.ToArray());
        }

        public Task DeleteAsync(
            IReadOnlyCollection<Guid> chunkIds,
            CancellationToken cancellationToken)
        {
            DeletedChunkIds = chunkIds.ToArray();

            foreach (var chunkId in chunkIds)
            {
                _chunkIds.Remove(chunkId);
            }

            return Task.CompletedTask;
        }

        public void AddChunkIds(IEnumerable<Guid> chunkIds)
        {
            foreach (var chunkId in chunkIds)
            {
                _chunkIds.Add(chunkId);
            }
        }

        public Task UpsertAsync(
            VectorRecord record,
            CancellationToken cancellationToken)
        {
            _chunkIds.Add(record.ChunkId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
            float[] embedding,
            int topK,
            DocumentSearchFilter? filter,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeDocumentIndexer : IDocumentIndexer
    {
        private readonly FakeVectorStore _vectorStore;
        private readonly Dictionary<Guid, IReadOnlyCollection<Guid>> _chunkIdsByDocument;

        public FakeDocumentIndexer(FakeVectorStore vectorStore, Dictionary<Guid, IReadOnlyCollection<Guid>> chunkIdsByDocument)
        {
            _vectorStore = vectorStore;
            _chunkIdsByDocument = chunkIdsByDocument;
        }

        public List<Guid> IndexedDocumentIds { get; } = [];

        public Task IndexAsync(
            Guid documentId,
            CancellationToken cancellationToken)
        {
            IndexedDocumentIds.Add(documentId);

            if (_chunkIdsByDocument.TryGetValue(documentId, out var chunkIds))
            {
                _vectorStore.AddChunkIds(chunkIds);
            }

            return Task.CompletedTask;
        }
    }
}
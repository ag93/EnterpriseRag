using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Infrastructure.Documents;

public class IndexReconciliationService : IIndexReconciliationService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IVectorStore _vectorStore;

    private readonly IDocumentIndexer _documentIndexer;
    public IndexReconciliationService(IDocumentRepository documentRepository, IVectorStore vectorStore, IDocumentIndexer documentIndexer)
    {
        _documentRepository = documentRepository;
        _vectorStore = vectorStore;
        _documentIndexer = documentIndexer;
    }

    public async Task<IndexReconciliationResult> ReconcileAsync(CancellationToken cancellationToken)
    {
        var sqliteChunkIds = await _documentRepository.GetAllChunkIdsAsync(cancellationToken);
        var vectorChunkIds = await _vectorStore.GetExistingChunkIdsAsync(cancellationToken);

        var sqliteIds = sqliteChunkIds.ToHashSet();
        var vectorIds = vectorChunkIds.ToHashSet();

        var missingChunkIds = sqliteIds.Except(vectorIds).ToArray();
        var orphanedChunkIds = vectorIds.Except(sqliteIds).ToArray();

        var result = new IndexReconciliationResult
        {
            MissingInVectorStore = missingChunkIds,
            OrphanedInVectorStore = orphanedChunkIds
        };

        if (missingChunkIds.Length > 0)
        {
            var documentIds = await _documentRepository.GetDocumentIdsForChunkIdsAsync(missingChunkIds, cancellationToken);

            foreach (var documentId in documentIds)
            {
                await _documentIndexer.IndexAsync(documentId, cancellationToken);
            }
        }

        if (orphanedChunkIds.Length > 0)
        {
            await _vectorStore.DeleteAsync(orphanedChunkIds, cancellationToken);
        }

        return result;
    }
}
using EnterpriseRag.Domain;

namespace EnterpriseRag.Application.Abstractions
{
    public interface IDocumentRepository
    {
        Task AddAsync(Document document, CancellationToken cancellationToken);
        Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(Guid documentId, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Guid>> GetAllChunkIdsAsync(CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Guid>> GetDocumentIdsForChunkIdsAsync(IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken);
    }
}

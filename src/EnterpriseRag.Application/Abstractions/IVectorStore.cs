using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions
{
    public interface IVectorStore
    {
        Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken);

        Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
            float[] embedding,
            int topK,
            DocumentSearchFilter? filter,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<Guid>> GetExistingChunkIdsAsync(CancellationToken cancellationToken);

        Task DeleteAsync(IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken);

    }
}

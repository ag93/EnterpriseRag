using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseRag.Infrastructure.Persistence;

public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DocumentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        await _dbContext.Documents.AddAsync(
            document,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Documents
            .FirstOrDefaultAsync(
                document => document.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return await _dbContext.DocumentChunks
            .Where(x => x.DocumentId == documentId)
            .OrderBy(x => x.ChunkIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetAllChunkIdsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.DocumentChunks
            .AsNoTracking()
            .Select(chunk => chunk.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetDocumentIdsForChunkIdsAsync(IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken)
    {
        if (chunkIds.Count == 0)
            return Array.Empty<Guid>();

        return await _dbContext.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunkIds.Contains(chunk.Id))
            .Select(chunk => chunk.DocumentId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
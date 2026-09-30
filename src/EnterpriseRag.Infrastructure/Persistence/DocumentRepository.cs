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

    public async Task AddAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        await _dbContext.Documents.AddAsync(
            document,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Document?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Documents
            .FirstOrDefaultAsync(
                document => document.Id == id,
                cancellationToken);
    }
}
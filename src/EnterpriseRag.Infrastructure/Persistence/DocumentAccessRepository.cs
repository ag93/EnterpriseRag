using EnterpriseRag.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseRag.Infrastructure.Persistence
{
    public class DocumentAccessRepository : IDocumentAccessRepository
    {
        private readonly ApplicationDbContext _dbContext;
        public DocumentAccessRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<Guid>> GetDocumentIdsForGroupsAsync(IReadOnlyCollection<string> groups, CancellationToken cancellationToken)
        {
            if (groups.Count == 0)
            {
                return Array.Empty<Guid>();
            }

            var documentIds = await EntityFrameworkQueryableExtensions.ToListAsync<Guid>(
                _dbContext.DocumentAccess
                    .Where(access => groups.Contains(access.GroupName))
                    .Select(access => access.DocumentId), cancellationToken);

            return documentIds;
        }
    }
}

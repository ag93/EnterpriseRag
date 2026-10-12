using EnterpriseRag.Application.Abstractions;

namespace EnterpriseRag.Application.Documents;
public class DocumentAuthorizationService : IDocumentAuthorizationService
{
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentAccessRepository _documentAccessRepository;

    public DocumentAuthorizationService(ICurrentUser currentUser, IDocumentAccessRepository documentAccessRepository)
    {
        _currentUser = currentUser;
        _documentAccessRepository = documentAccessRepository;
    }

    public async Task<IReadOnlyCollection<Guid>> GetAuthorizedDocumentIdsAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Array.Empty<Guid>();
        }

        var groups = _currentUser.Groups
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (groups.Length == 0)
        {
            return Array.Empty<Guid>();
        }

        return await _documentAccessRepository.GetDocumentIdsForGroupsAsync(groups, cancellationToken);
    }
}

namespace EnterpriseRag.Application.Abstractions
{
    public interface IDocumentAuthorizationService
    {
        Task<IReadOnlyCollection<Guid>> GetAuthorizedDocumentIdsAsync(CancellationToken cancellationToken);
    }
}
namespace EnterpriseRag.Application.Abstractions
{
    public interface IDocumentAccessRepository
    {
        Task<IReadOnlyCollection<Guid>> GetDocumentIdsForGroupsAsync(
            IReadOnlyCollection<string> groups,
            CancellationToken cancellationToken);
    }
}

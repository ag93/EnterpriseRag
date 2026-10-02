namespace EnterpriseRag.Application.Abstractions
{
    public interface IDocumentIndexer
    {
        Task IndexAsync(Guid documentId, CancellationToken cancellationToken);
    }
}

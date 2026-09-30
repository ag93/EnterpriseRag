namespace EnterpriseRag.Application.Abstractions
{
    public interface IDocumentStorage
    {
        Task<string> SaveAsync(Stream content, string filename, CancellationToken cancellationToken);

        Task<Stream> OpenAsync(string path, CancellationToken cancellationToken);
    }
}

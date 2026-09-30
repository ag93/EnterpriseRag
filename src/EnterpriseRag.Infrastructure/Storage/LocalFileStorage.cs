using EnterpriseRag.Application.Abstractions;


namespace EnterpriseRag.Infrastructure.Storage
{
    public class LocalFileStorage : IDocumentStorage
    {
        private readonly string _rootPath;

        public LocalFileStorage(string rootPath)
        {
            _rootPath = rootPath;
            Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
        {
            var fileId = Guid.NewGuid();
            var extension = Path.GetExtension(fileName);
            var storedFileName = $"{fileId}{extension}";
            var fullPath = Path.Combine(_rootPath, storedFileName);

            await using var fileStream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            await content.CopyToAsync(fileStream, cancellationToken);

            return storedFileName;
        }

        public Task<Stream> OpenAsync(string path, CancellationToken cancellationToken)
        {
            var fullPath = Path.Combine(_rootPath, path);

            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            return Task.FromResult(stream);
        }
    }
}

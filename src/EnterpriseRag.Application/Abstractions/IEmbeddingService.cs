namespace EnterpriseRag.Application.Abstractions
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string texts, CancellationToken cancellationToken);
    }
}

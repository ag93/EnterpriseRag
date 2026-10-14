namespace EnterpriseRag.Application.Models;

public class IndexReconciliationResult
{
    public IReadOnlyCollection<Guid> MissingInVectorStore { get; init; } = Array.Empty<Guid>();

    public IReadOnlyCollection<Guid> OrphanedInVectorStore { get; init; } = Array.Empty<Guid>();
}
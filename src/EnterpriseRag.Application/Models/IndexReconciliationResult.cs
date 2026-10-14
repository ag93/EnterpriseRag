namespace EnterpriseRag.Application.Models;

/// <summary>
/// Describes inconsistencies detected between the authoritative SQLite data
/// and the derived vector-store index before reconciliation repairs are applied.
/// </summary>
public class IndexReconciliationResult
{
    /// <summary>
    /// Gets the chunk IDs present in SQLite but missing from the vector store
    /// when reconciliation began.
    /// </summary>
    public IReadOnlyCollection<Guid> MissingInVectorStore { get; init; }
        = Array.Empty<Guid>();

    /// <summary>
    /// Gets the chunk IDs present in the vector store but absent from SQLite
    /// when reconciliation began.
    /// </summary>
    public IReadOnlyCollection<Guid> OrphanedInVectorStore { get; init; }
        = Array.Empty<Guid>();

    /// <summary>
    /// Gets a value indicating whether any inconsistencies were detected
    /// before repair was attempted.
    /// </summary>
    public bool HadInconsistencies =>
        MissingInVectorStore.Count > 0 ||
        OrphanedInVectorStore.Count > 0;
}
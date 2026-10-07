using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions;

public interface IReranker
{
    Task<IReadOnlyList<SearchResult>> RerankAsync(string query, IReadOnlyList<SearchResult> results, CancellationToken cancellationToken);
}
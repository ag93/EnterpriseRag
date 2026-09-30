using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions
{
    public interface IDocumentSearch
    {
        Task<IReadOnlyList<SearchResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
    }
}

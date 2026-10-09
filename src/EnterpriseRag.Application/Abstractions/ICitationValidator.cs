using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions
{
    public interface ICitationValidator
    {
        Task<CitationValidationResult> ValidateAsync(string answer,
            IReadOnlyList<RagCitation> citations,
            IReadOnlyList<SearchResult> retrievedResults,
            CancellationToken cancellationToken);
    }
}
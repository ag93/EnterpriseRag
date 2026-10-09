using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Infrastructure
{
    public class CitationValidator : ICitationValidator
    {
        public Task<CitationValidationResult> ValidateAsync(
            string answer,
            IReadOnlyList<RagCitation> citations,
            IReadOnlyList<SearchResult> retrievedResults,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var unsupportedCitations = citations
                .Where(citation => !retrievedResults.Any(result =>
                    result.DocumentId == citation.DocumentId &&
                    result.ChunkId == citation.ChunkId))
                .ToList();

            var result = new CitationValidationResult
            {
                IsSupported = unsupportedCitations.Count == 0,
                UnsupportedCitations = unsupportedCitations
            };

            return Task.FromResult(result);
        }
    }
}

using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Infrastructure.Citation
{
    public class CitationValidator : ICitationValidator
    {
        private readonly ICitationSupportChecker _supportChecker;

        public CitationValidator(ICitationSupportChecker supportChecker)
        {
            _supportChecker = supportChecker;
        }

        public async Task<CitationValidationResult> ValidateAsync(
            string answer,
            IReadOnlyList<RagCitation> citations,
            IReadOnlyList<SearchResult> retrievedResults,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var unsupportedCitations = new List<RagCitation>();

            foreach (var citation in citations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var matchingResult = retrievedResults.FirstOrDefault(result =>
                    result.DocumentId == citation.DocumentId &&
                    result.ChunkId == citation.ChunkId);

                if (matchingResult is null)
                {
                    unsupportedCitations.Add(citation);
                    continue;
                }

                var isSupported = await _supportChecker.IsSupportedAsync(
                    answer,
                    matchingResult.Content,
                    cancellationToken);

                if (!isSupported)
                {
                    unsupportedCitations.Add(citation);
                }
            }

            return new CitationValidationResult
            {
                IsSupported = unsupportedCitations.Count == 0,
                UnsupportedCitations = unsupportedCitations
            };
        }
    }
}
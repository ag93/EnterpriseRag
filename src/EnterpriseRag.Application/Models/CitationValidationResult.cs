namespace EnterpriseRag.Application.Models
{
    public class CitationValidationResult
    {
        public bool IsSupported { get; init; }
        public IReadOnlyList<RagCitation> UnsupportedCitations { get; init; } = [];
        public int TotalCitationsCount { get; init; }
        public int SupportedCitationsCount { get; init; }
    }
}

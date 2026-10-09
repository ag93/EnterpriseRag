namespace EnterpriseRag.Application.Models
{
    public class CitationValidationResult
    {
        public bool IsSupported { get; init; }
        public IReadOnlyList<RagCitation> UnsupportedCitations { get; init; } = [];
    }
}

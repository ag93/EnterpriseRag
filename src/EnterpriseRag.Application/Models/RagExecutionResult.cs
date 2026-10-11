namespace EnterpriseRag.Application.Models
{
    public class RagExecutionResult
    {
        public RagAnswer Answer { get; init; } = new();
        public CitationValidationResult CitationValidation { get; init; } = new();
    }
}

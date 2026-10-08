namespace EnterpriseRag.IntegrationTests.RAG;

public class RagEvaluationResult
{
    public string Name { get; init; } = string.Empty;
    public string Question { get; init; } = string.Empty;
    public bool AnswerExpected { get; init; }
    public bool AnswerContainsExpectedFragment { get; init; }
    public bool HasCitations { get; init; }
    public bool CitationsMatchExpectedDocuments { get; init; }
}
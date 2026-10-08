namespace EnterpriseRag.IntegrationTests.RAG
{
    public class RagEvaluationTestCase
    {
        public string Name { get; init; } = string.Empty;
        public string Question { get; init; } = string.Empty;
        public bool AnswerExpected { get; init; }
        public string[] ExpectedAnswerFragments { get; init; } = [];
        public string[] ExpectedFileNames { get; init; } = [];
    }
}

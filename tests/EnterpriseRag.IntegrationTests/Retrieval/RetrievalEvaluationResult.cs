namespace EnterpriseRag.IntegrationTests.Retrieval
{
    public class RetrievalEvaluationResult
    {
        public string Name { get; init; } = string.Empty;
        public string Query { get; init; } = string.Empty;
        public bool AnswerExpected { get; init; }
        public bool HitAt5 { get; init; }
        public int? FirstRelevantRank { get; init; }
        public double ReciprocalRank =>
        FirstRelevantRank.HasValue
            ? 1.0 / FirstRelevantRank.Value
            : 0.0;
    }
}

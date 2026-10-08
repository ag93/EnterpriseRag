namespace EnterpriseRag.Application.Retrieval;

public class RetrievalTestCase
{
    public string Name { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;

    public string[] ExpectedFileNames { get; init; } = [];

    public bool AnswerExpected { get; init; }
}
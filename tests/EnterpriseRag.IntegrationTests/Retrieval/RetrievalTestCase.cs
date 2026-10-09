namespace EnterpriseRag.Application.Retrieval;

public class RetrievalTestCase
{
    public string Name { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public DocumentSearchFilter? Filter { get; init; }

    public string[] ExpectedFileNames { get; init; } = [];

    public bool AnswerExpected { get; init; }
}
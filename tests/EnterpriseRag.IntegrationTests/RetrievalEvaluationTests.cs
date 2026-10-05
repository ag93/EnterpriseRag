using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Application.Retrieval;
using EnterpriseRag.Infrastructure.AI;
using EnterpriseRag.Infrastructure.VectorStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Qdrant.Client;
using Xunit.Abstractions;

namespace EnterpriseRag.IntegrationTests.Retrieval;

public class RetrievalEvaluationTests
{
    private readonly ITestOutputHelper _output;

    public RetrievalEvaluationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Evaluate_Retrieval_Baseline()
    {
        var services = new ServiceCollection();

        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(
            client =>
            {
                client.BaseAddress =
                    new Uri("http://localhost:11434");
            });

        services.AddSingleton(new QdrantClient("localhost", 6334));

        services.AddScoped<IVectorStore, QdrantVectorStore>();

        services.AddScoped<IDocumentSearch, DocumentSearch>();

        await using var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        var documentSearch = scope.ServiceProvider.GetRequiredService<IDocumentSearch>();

        var results = new List<RetrievalEvaluationResult>();

        foreach (var testCase in RetrievalTestCases.All)
        {
            var searchResults =
                await documentSearch.SearchAsync(
                    new SearchRequest
                    {
                        Query = testCase.Query,
                        TopK = 5
                    },
                    CancellationToken.None);

            var firstRelevantRank =
                searchResults
                    .Select((result, index) => new
                    {
                        Result = result,
                        Rank = index + 1
                    })
                    .Where(x =>
                        testCase.ExpectedFileNames.Contains(
                            x.Result.FileName,
                            StringComparer.OrdinalIgnoreCase))
                    .Select(x => (int?)x.Rank)
                    .FirstOrDefault();

            var hitAt5 =
                firstRelevantRank.HasValue;

            results.Add(
                new RetrievalEvaluationResult
                {
                    Name = testCase.Name,
                    Query = testCase.Query,
                    AnswerExpected = testCase.AnswerExpected,
                    HitAt5 = hitAt5,
                    FirstRelevantRank = firstRelevantRank
                });
        }

        foreach (var result in results)
        {
            _output.WriteLine(
                $"{result.Name} | " +
                $"Hit@5={result.HitAt5} | " +
                $"FirstRelevantRank={result.FirstRelevantRank?.ToString() ?? "-"}");
        }

        var expectedRetrievalCases =
            RetrievalTestCases.All
                .Where(x => x.ExpectedFileNames.Length > 0)
                .ToList();

        var hitCount =
            results.Count(x => x.HitAt5);

        var hitRate =
            expectedRetrievalCases.Count == 0
                ? 0
                : (double)hitCount /
                  expectedRetrievalCases.Count;

        var meanReciprocalRank = expectedRetrievalCases.Count == 0 
            ? 0 
            : results
            .Where(x => x.FirstRelevantRank.HasValue)
            .Average(x => x.ReciprocalRank);

        _output.WriteLine("");
        _output.WriteLine(
            $"Hit@5: {hitCount}/{expectedRetrievalCases.Count} " +
            $"({hitRate:P1})");

        _output.WriteLine(
            $"MRR: {meanReciprocalRank:F3}");
    }
}
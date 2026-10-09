using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Application.Retrieval;
using EnterpriseRag.Infrastructure.AI;
using EnterpriseRag.Infrastructure.Search;
using EnterpriseRag.Infrastructure.VectorStore;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<IReranker, KeywordReranker>();

        await using var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        //Default -> Includes reranking.
        //var documentSearch = scope.ServiceProvider.GetRequiredService<IDocumentSearch>();

        // Without Reranking. 
        // Disable reranking so this test measures raw semantic retrieval.
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
        var reranker = scope.ServiceProvider.GetRequiredService<IReranker>();
        var documentSearch = new DocumentSearch(embeddingService, vectorStore, reranker, enableReranking: false);

        var results = new List<RetrievalEvaluationResult>();
        var diversityCounts = new List<int>();
        foreach (var testCase in RetrievalTestCases.All)
        {
            var searchResults =
                await documentSearch.SearchAsync(
                    new SearchRequest
                    {
                        Query = testCase.Query,
                        TopK = 5,
                        CandidateCount = 20,
                        Filter = testCase.Filter
                    },
                    CancellationToken.None);

            var uniqueDocumentCount =
                searchResults
                    .Select(result => result.DocumentId)
                    .Distinct()
                    .Count();
            diversityCounts.Add(uniqueDocumentCount);
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

            var highestSemanticScore =
                searchResults.Max(result => result.Score);

            var lowestTop5SemanticScore =
                searchResults.Min(result => result.Score);

            _output.WriteLine(
                $"    HighestSemanticScore={highestSemanticScore:F3} | " +
                $"LowestTop5SemanticScore={lowestTop5SemanticScore:F3}");

            var hitAt5 =
                firstRelevantRank.HasValue;

            _output.WriteLine(
                $"\n{testCase.Name} | " +
                $"Hit@5={hitAt5} | " +
                $"FirstRelevantRank={firstRelevantRank?.ToString() ?? "-"} | " +
                $"UniqueDocuments={uniqueDocumentCount}");
            foreach (var result in searchResults)
            {
                _output.WriteLine(
                    $"    {result.FileName} | " +
                    $"SemanticScore={result.Score:F3}");
            }

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

        var averageUniqueDocuments = diversityCounts.Average();

        _output.WriteLine($"\nAverageUniqueDocumentsInTop5: {averageUniqueDocuments:F2}");

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
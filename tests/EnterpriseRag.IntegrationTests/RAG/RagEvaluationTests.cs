using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Application.RAG;
using EnterpriseRag.Infrastructure.AI;
using EnterpriseRag.Infrastructure.Authorization;
using EnterpriseRag.Infrastructure.Citation;
using EnterpriseRag.Infrastructure.Persistence;
using EnterpriseRag.Infrastructure.Search;
using EnterpriseRag.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;
using Xunit.Abstractions;

namespace EnterpriseRag.IntegrationTests.RAG;

public class RagEvaluationTests
{
    private const int TopKCount = 5;
    private const int CandidateCount = 20;

    private readonly ITestOutputHelper _output;

    public RagEvaluationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Evaluate_RAG_Baseline()
    {
        var retrievalResults = new Dictionary<string, (IReadOnlyList<SearchResult> Baseline, IReadOnlyList<SearchResult> Reranked)>();
        var services = new ServiceCollection();

        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(
            client =>
            {
                client.BaseAddress = new Uri("http://localhost:11434");
            });

        services.AddHttpClient<IChatService, OllamaChatService>(
            client =>
            {
                client.BaseAddress = new Uri("http://localhost:11434");
            });

        services.AddHttpClient<ICitationSupportChecker, OllamaCitationSupportChecker>(
            client =>
            {
                client.BaseAddress = new Uri("http://localhost:11434");
            });

        services.AddSingleton(new QdrantClient("localhost", 6334));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Testing.json", optional: false)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        var databasePath = configuration["Evaluation:DatabasePath"];
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new InvalidOperationException("Evaluation:DatabasePath is missing from appsettings.Testing.json.");
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("The RAG evaluation database was not found.", databasePath);
        
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddScoped<IVectorStore, QdrantVectorStore>();
        services.AddScoped<IReranker, KeywordReranker>();
        services.AddScoped<IDocumentSearch, DocumentSearch>();
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<ICitationValidator, CitationValidator>();
        services.AddScoped<ICurrentUser, LocalCurrentUser>();
        services.AddScoped<IDocumentAuthorizationService, DocumentAuthorizationService>();
        services.AddScoped<IDocumentAccessRepository, DocumentAccessRepository>();

        await using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var ragService = scope.ServiceProvider.GetRequiredService<IRagService>();
        var documentSearch = scope.ServiceProvider.GetRequiredService<IDocumentSearch>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        var results = new List<RagEvaluationResult>();
        var totalCitationsValidated = 0;
        var supportedCitations = 0;
        var rejectedCitations = 0;

        foreach (var testCase in RagEvaluationTestCases.TestCases)
        {
            await WriteRetrievalDiagnosticsAsync(
                testCase,
                embeddingService,
                vectorStore,
                documentSearch,
                retrievalResults);

            var execution = await ragService.AskWithDiagnosticsAsync(testCase.Question, CancellationToken.None);

            var answer = execution.Answer;
            var citationValidation = execution.CitationValidation;

            _output.WriteLine(
                $"Citations validated: {citationValidation.SupportedCitationsCount}/" +
                $"{citationValidation.TotalCitationsCount} supported; " +
                $"{citationValidation.UnsupportedCitations.Count} rejected.");

            totalCitationsValidated += citationValidation.TotalCitationsCount;
            supportedCitations += citationValidation.SupportedCitationsCount;
            rejectedCitations += citationValidation.UnsupportedCitations.Count;

            var answerContainsExpectedFragments =
                testCase.ExpectedAnswerFragments.Length > 0 &&
                testCase.ExpectedAnswerFragments.All(
                    fragment => answer.Answer.Contains(
                        fragment,
                        StringComparison.OrdinalIgnoreCase));

            var hasCitations = answer.Citations.Count > 0;

            var citationsMatchExpectedDocuments =
                !testCase.AnswerExpected
                    ? answer.Citations.Count == 0
                    : testCase.ExpectedFileNames.Length == 0
                        ? answer.Citations.Count == 0
                        : answer.Citations.All(
                              citation => testCase.ExpectedFileNames.Contains(
                                  citation.FileName,
                                  StringComparer.OrdinalIgnoreCase))
                          &&
                          testCase.ExpectedFileNames.All(
                              expectedFileName => answer.Citations.Any(
                                  citation => string.Equals(
                                      citation.FileName,
                                      expectedFileName,
                                      StringComparison.OrdinalIgnoreCase)));

            results.Add(new RagEvaluationResult
            {
                Name = testCase.Name,
                Question = testCase.Question,
                AnswerExpected = testCase.AnswerExpected,
                AnswerContainsExpectedFragment = answerContainsExpectedFragments,
                HasCitations = hasCitations,
                CitationsMatchExpectedDocuments = citationsMatchExpectedDocuments
            });

            WriteAnswerDiagnostics(
                testCase,
                answer,
                answerContainsExpectedFragments,
                citationsMatchExpectedDocuments);
        }
        WriteAggregateRetrievalSummary(RagEvaluationTestCases.TestCases, retrievalResults);
        WriteEvaluationSummary(results, totalCitationsValidated, supportedCitations, rejectedCitations);
    }

    private async Task WriteRetrievalDiagnosticsAsync(RagEvaluationTestCase testCase,
                                                      IEmbeddingService embeddingService,
                                                      IVectorStore vectorStore,
                                                      IDocumentSearch documentSearch,
                                                      Dictionary<string, (IReadOnlyList<SearchResult> Baseline, IReadOnlyList<SearchResult> Reranked)> retrievalResults)
    {
        _output.WriteLine("");
        _output.WriteLine($"Retrieval diagnostics: {testCase.Name}");
        _output.WriteLine($"  Query: {testCase.Question}");

        // Run vector search directly to capture the ranking before reranking.
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(
            testCase.Question,
            CancellationToken.None);

        var baselineVectorResults = await vectorStore.SearchAsync(
            queryEmbedding,
            TopKCount,
            null,
            CancellationToken.None);

        var baselineResults = baselineVectorResults
            .Select(result => new SearchResult
            {
                DocumentId = result.DocumentId,
                ChunkId = result.ChunkId,
                FileName = result.FileName,
                Content = result.Content,
                PageNumber = result.PageNumber,
                Score = result.Score
            })
            .ToList();

        // DocumentSearch performs the normal retrieval and reranking pipeline.
        var rerankedResults = await documentSearch.SearchAsync(
            new SearchRequest
            {
                Query = testCase.Question,
                TopK = TopKCount,
                CandidateCount = CandidateCount
            },
            CancellationToken.None);
        retrievalResults[testCase.Name] = (baselineResults, rerankedResults);
        WriteExpectedChunkRanks(testCase, baselineResults, rerankedResults);
        WriteRetrievalMetrics(testCase, baselineResults, rerankedResults);
        WriteMrrDiagnostics(testCase, baselineResults, rerankedResults);
    }

    private void WriteExpectedChunkRanks(
        RagEvaluationTestCase testCase,
        IReadOnlyList<SearchResult> baselineResults,
        IReadOnlyList<SearchResult> rerankedResults)
    {
        if (!testCase.AnswerExpected ||
            testCase.ExpectedAnswerFragments.Length == 0)
        {
            _output.WriteLine(
                "  Answer-bearing chunk comparison skipped " +
                "(no expected answer fragments).");

            return;
        }

        _output.WriteLine(
            "  Expected-answer fragment ranks (before -> after):");

        foreach (var fragment in testCase.ExpectedAnswerFragments)
        {
            var baselineRank = FindFirstChunkRank(baselineResults, fragment);
            var rerankedRank = FindFirstChunkRank(rerankedResults, fragment);

            _output.WriteLine(
                $"    \"{fragment}\": " +
                $"{FormatRank(baselineRank)} -> {FormatRank(rerankedRank)}");
        }
    }

    private static int FindFirstChunkRank(IReadOnlyList<SearchResult> results, string expectedFragment)
    {
        for (var index = 0; index < results.Count; index++)
        {
            if (results[index].Content.Contains(
                expectedFragment,
                StringComparison.OrdinalIgnoreCase))
            {
                return index + 1;
            }
        }

        return -1;
    }

    private static string FormatRank(int rank)
    {
        return rank == -1 ? "Not found" : rank.ToString();
    }

    private void WriteAnswerDiagnostics(RagEvaluationTestCase testCase, RagAnswer answer, bool answerContainsExpectedFragments, bool citationsMatchExpectedDocuments)
    {
        _output.WriteLine("");
        _output.WriteLine(testCase.Name);
        _output.WriteLine($"Question: {testCase.Question}");
        _output.WriteLine($"Answer: {answer.Answer}");
        _output.WriteLine($"Citations: {answer.Citations.Count}");

        foreach (var citation in answer.Citations)
        {
            _output.WriteLine(
                $"  {citation.FileName} | Page={citation.PageNumber}");
        }

        _output.WriteLine(
            $"AnswerContainsExpectedFragments: {answerContainsExpectedFragments}");

        _output.WriteLine(
            $"CitationsMatchExpectedDocuments: {citationsMatchExpectedDocuments}");
    }


    private void WriteRetrievalMetrics(RagEvaluationTestCase testCase, IReadOnlyList<SearchResult> baselineResults, IReadOnlyList<SearchResult> rerankedResults)
    {
        if (!testCase.AnswerExpected ||
            testCase.ExpectedAnswerFragments.Length == 0)
        {
            _output.WriteLine( 
                "   Retrieval metrics skipped: no expected answer fragments.");
            return;
        }

        int[] cutoffs = [1, 3, 5];

        _output.WriteLine("   Answer-bearing chunk retrieval metrics:");

        foreach (int cutoff in cutoffs)
        {
            int baselineHits = CountRetrievedFragments(
                testCase.ExpectedAnswerFragments,
                baselineResults.Take(cutoff));

            int rerankedHits = CountRetrievedFragments(
                testCase.ExpectedAnswerFragments,
                rerankedResults.Take(cutoff));

            int totalFragments = testCase.ExpectedAnswerFragments.Length;

            _output.WriteLine(
                $"     Hit@{cutoff}: " +
                $"before {baselineHits}/{totalFragments}, " +
                $"after {rerankedHits}/{totalFragments}");
        }
    }

    private static int CountRetrievedFragments(IReadOnlyList<string> expectedFragments, IEnumerable<SearchResult> results)
    {
        var retrievedContent = results
            .Select(result => result.Content)
            .ToArray();

        return expectedFragments.Count(fragment =>
            retrievedContent.Any(content =>
                content.Contains(
                    fragment,
                    StringComparison.OrdinalIgnoreCase)));
    }


    private static double CalculateReciprocalRank(
        IReadOnlyList<string> expectedFragments,
        IReadOnlyList<SearchResult> results)
    {
        if (expectedFragments.Count == 0)
        {
            return 0.0;
        }

        for (int i = 0; i < results.Count; i++)
        {
            bool containsExpectedFragment = expectedFragments.Any(fragment =>
                results[i].Content.Contains(
                    fragment,
                    StringComparison.OrdinalIgnoreCase));

            if (containsExpectedFragment)
            {
                return 1.0 / (i + 1);
            }
        }

        return 0.0;
    }

    private void WriteMrrDiagnostics(
        RagEvaluationTestCase testCase,
        IReadOnlyList<SearchResult> baselineResults,
        IReadOnlyList<SearchResult> rerankedResults)
    {
        if (!testCase.AnswerExpected ||
            testCase.ExpectedAnswerFragments.Length == 0)
        {
            _output.WriteLine(
                "   MRR skipped: no expected answer fragments.");
            return;
        }

        double baselineMrr = CalculateReciprocalRank(
            testCase.ExpectedAnswerFragments,
            baselineResults);

        double rerankedMrr = CalculateReciprocalRank(
            testCase.ExpectedAnswerFragments,
            rerankedResults);

        _output.WriteLine(
            $"   MRR: before {baselineMrr:F3}, " +
            $"after {rerankedMrr:F3}, " +
            $"change {rerankedMrr - baselineMrr:+0.000;-0.000;0.000}");
    }


    private void WriteAggregateRetrievalSummary(
        IReadOnlyList<RagEvaluationTestCase> testCases,
        IReadOnlyDictionary<string, (
            IReadOnlyList<SearchResult> Baseline,
            IReadOnlyList<SearchResult> Reranked)> retrievalResults)
    {
        int[] cutoffs = [1, 3, 5];

        var eligibleCases = testCases
            .Where(testCase =>
                testCase.AnswerExpected &&
                testCase.ExpectedAnswerFragments.Length > 0)
            .ToList();

        if (eligibleCases.Count == 0)
        {
            _output.WriteLine("No eligible cases for retrieval summary.");
            return;
        }

        double baselineMrr = 0;
        double rerankedMrr = 0;

        foreach (var testCase in eligibleCases)
        {
            var results = retrievalResults[testCase.Name];

            baselineMrr += CalculateReciprocalRank(
                testCase.ExpectedAnswerFragments,
                results.Baseline);

            rerankedMrr += CalculateReciprocalRank(
                testCase.ExpectedAnswerFragments,
                results.Reranked);
        }

        baselineMrr /= eligibleCases.Count;
        rerankedMrr /= eligibleCases.Count;

        _output.WriteLine("");
        _output.WriteLine("Aggregate Retrieval Summary");
        _output.WriteLine($"Eligible questions: {eligibleCases.Count}");
        _output.WriteLine(
            $"MRR: baseline {baselineMrr:F3}, " +
            $"reranked {rerankedMrr:F3}, " +
            $"change {rerankedMrr - baselineMrr:+0.000;-0.000;0.000}");

        foreach (int cutoff in cutoffs)
        {
            double baselineRecall = 0;
            double rerankedRecall = 0;

            foreach (var testCase in eligibleCases)
            {
                var results = retrievalResults[testCase.Name];
                int fragmentCount = testCase.ExpectedAnswerFragments.Length;

                baselineRecall += (double)CountRetrievedFragments(
                    testCase.ExpectedAnswerFragments,
                    results.Baseline.Take(cutoff)) / fragmentCount;

                rerankedRecall += (double)CountRetrievedFragments(
                    testCase.ExpectedAnswerFragments,
                    results.Reranked.Take(cutoff)) / fragmentCount;
            }

            baselineRecall /= eligibleCases.Count;
            rerankedRecall /= eligibleCases.Count;

            _output.WriteLine(
                $"Evidence Recall@{cutoff}: " +
                $"baseline {baselineRecall:P1}, " +
                $"reranked {rerankedRecall:P1}, " +
                $"change {rerankedRecall - baselineRecall:+0.0%;-0.0%;0.0%}");
        }
    }

    private void WriteEvaluationSummary(IReadOnlyList<RagEvaluationResult> results, int totalCitationsValidated, int supportedCitations, int rejectedCitations)
    {
        var answerableCases = results
            .Where(result => result.AnswerExpected)
            .ToList();

        var unanswerableCases = results
            .Where(result => !result.AnswerExpected)
            .ToList();

        var answerableCorrect = answerableCases.Count(result =>
            result.AnswerContainsExpectedFragment &&
            result.HasCitations &&
            result.CitationsMatchExpectedDocuments);

        var refusalCorrect = unanswerableCases.Count(result =>
            !result.AnswerContainsExpectedFragment &&
            result.CitationsMatchExpectedDocuments);

        _output.WriteLine("");
        _output.WriteLine("RAG Evaluation Summary");
        _output.WriteLine(
            $"Answerable cases: {answerableCorrect}/{answerableCases.Count}");

        _output.WriteLine(
            $"Unanswerable cases handled correctly: {refusalCorrect}/{unanswerableCases.Count}");

        Assert.Equal(answerableCases.Count, answerableCorrect);
        Assert.Equal(unanswerableCases.Count, refusalCorrect);

        _output.WriteLine("");
        _output.WriteLine("Citation Validation Summary");
        _output.WriteLine($"Total citations validated: {totalCitationsValidated}");
        _output.WriteLine($"Supported citations: {supportedCitations}");
        _output.WriteLine($"Rejected citations: {rejectedCitations}");

        var supportRate = totalCitationsValidated == 0
            ? 0
            : 100.0 * supportedCitations / totalCitationsValidated;

        _output.WriteLine($"Citation support rate: {supportRate:F1}%");
    }
}
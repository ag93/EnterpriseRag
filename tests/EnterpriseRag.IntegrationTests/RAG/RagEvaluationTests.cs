using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Application.RAG;
using EnterpriseRag.Infrastructure.AI;
using EnterpriseRag.Infrastructure.Search;
using EnterpriseRag.Infrastructure.VectorStore;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;
using Xunit;
using Xunit.Abstractions;

namespace EnterpriseRag.IntegrationTests.RAG;

public class RagEvaluationTests
{
    private readonly ITestOutputHelper _output;

    public RagEvaluationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Evaluate_RAG_Baseline()
    {
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

        services.AddSingleton(new QdrantClient("localhost", 6334));

        services.AddScoped<IVectorStore, QdrantVectorStore>();
        services.AddScoped<IReranker, KeywordReranker>();
        services.AddScoped<IDocumentSearch, DocumentSearch>();
        services.AddScoped<IRagService, RagService>();

        await using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var ragService = scope.ServiceProvider.GetRequiredService<IRagService>();
        var documentSearch = scope.ServiceProvider.GetRequiredService<IDocumentSearch>();
        var results = new List<RagEvaluationResult>();

        foreach (var testCase in RagEvaluationTestCases.TestCases)
        {
            var answer =
                await ragService.AskAsync(testCase.Question, CancellationToken.None);

            var answerContainsExpectedFragments =
                testCase.ExpectedAnswerFragments.Length > 0 &&
                testCase.ExpectedAnswerFragments.All(
                    fragment =>
                        answer.Answer.Contains(
                            fragment,
                            StringComparison.OrdinalIgnoreCase));

            var hasCitations = answer.Citations.Count > 0;

            var citationsMatchExpectedDocuments =
                testCase.ExpectedFileNames.Length == 0
                    ? answer.Citations.Count == 0
                    : answer.Citations.All(
                        citation =>
                            testCase.ExpectedFileNames.Contains(
                                citation.FileName,
                                StringComparer.OrdinalIgnoreCase));

            var result =
                new RagEvaluationResult
                {
                    Name = testCase.Name,
                    Question = testCase.Question,
                    AnswerExpected = testCase.AnswerExpected,
                    AnswerContainsExpectedFragment =
                        answerContainsExpectedFragments,
                    HasCitations = hasCitations,
                    CitationsMatchExpectedDocuments =
                        citationsMatchExpectedDocuments
                };

            results.Add(result);

            _output.WriteLine("");
            _output.WriteLine(testCase.Name);
            _output.WriteLine($"Question: {testCase.Question}");
            _output.WriteLine($"Answer: {answer.Answer}");
            _output.WriteLine($"Citations: {answer.Citations.Count}");

            foreach (var citation in answer.Citations)
            {
                _output.WriteLine(
                    $"    {citation.FileName} | " +
                    $"Page={citation.PageNumber}");
            }

            _output.WriteLine(
                $"AnswerContainsExpectedFragments: " +
                $"{answerContainsExpectedFragments}");

            _output.WriteLine(
                $"CitationsMatchExpectedDocuments: " +
                $"{citationsMatchExpectedDocuments}");
        }

        var answerableCases = RagEvaluationTestCases.TestCases.Where(x => x.AnswerExpected).ToList();
        var unanswerableCases = RagEvaluationTestCases.TestCases.Where(x => !x.AnswerExpected).ToList();
        var answerableResults = results.Where(x => x.AnswerExpected).ToList();

        var unanswerableResults = results.Where(x => !x.AnswerExpected).ToList();

        var answerableCorrect =
            answerableResults.Count(x =>
                x.AnswerContainsExpectedFragment &&
                x.HasCitations &&
                x.CitationsMatchExpectedDocuments);

        var refusalCorrect =
            unanswerableResults.Count(x =>
                !x.AnswerContainsExpectedFragment &&
                x.CitationsMatchExpectedDocuments);

        _output.WriteLine("");
        _output.WriteLine("RAG Evaluation Summary");
        _output.WriteLine(
            $"Answerable cases: " +
            $"{answerableCorrect}/{answerableCases.Count}");

        _output.WriteLine(
            $"Unanswerable cases handled correctly: " +
            $"{refusalCorrect}/{unanswerableCases.Count}");

        Assert.Equal(answerableCases.Count, answerableCorrect);

        Assert.Equal(unanswerableCases.Count, refusalCorrect);
    }
}
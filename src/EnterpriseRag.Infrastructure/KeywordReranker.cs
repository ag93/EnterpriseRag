using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Infrastructure.Search;

public class KeywordReranker : IReranker
{
    private const double SemanticWeight = 0.70;
    private const double KeywordWeight = 0.20;
    private const double PhraseWeight = 0.10;
    public Task<IReadOnlyList<SearchResult>> RerankAsync(string query, IReadOnlyList<SearchResult> results, CancellationToken cancellationToken)
    {
        var queryTerms = Tokenize(query);
        var reranked = results
            .Select(result =>
            {
                var keywordScore = CalculateKeywordScore(result.Content, queryTerms);
                var phraseScore = CalculatePhraseScore(query, result.Content);

                var finalScore =
                    (SemanticWeight * result.Score) +
                    (KeywordWeight * keywordScore) +
                    (PhraseWeight * phraseScore);

                return new
                {
                    Result = result,
                    FinalScore = finalScore
                };
            })
            .OrderByDescending(x => x.FinalScore)
            .ThenByDescending(x => x.Result.Score)
            .Select(x => x.Result)
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchResult>>(reranked);
    }

    private static double CalculateKeywordScore(string content, IReadOnlyList<string> queryTerms)
    {
        if (queryTerms.Count == 0)
            return 0;
        
        var contentTerms = Tokenize(content);
        var matches = queryTerms.Count(contentTerms.Contains);
        return (double)matches / queryTerms.Count;
    }

    private static double CalculatePhraseScore(string query, string content)
    {
        var normalizedQuery = Normalize(query);
        var normalizedContent = Normalize(content);
        return normalizedContent.Contains(normalizedQuery,StringComparison.Ordinal)
                ? 1.0
                : 0.0;
    }

    private static string Normalize(string text)
    {
        return string.Join(
            ' ',
            text
                .ToLowerInvariant()
                .Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries));
    }

    private static IReadOnlyList<string> Tokenize(string text)
    {
        return text
            .ToLowerInvariant()
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries)
            .Select(
                word => new string(word.Where(char.IsLetterOrDigit).ToArray()))
            .Where(word => word.Length >= 3)
            .Distinct()
            .ToList();
    }
}
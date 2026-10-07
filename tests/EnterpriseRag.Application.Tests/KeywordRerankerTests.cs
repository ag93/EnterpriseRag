using EnterpriseRag.Application.Models;
using EnterpriseRag.Infrastructure.Search;

namespace EnterpriseRag.Application.Tests;

public class KeywordRerankerTests
{
    [Fact]
    public async Task Rerank_Should_Put_Most_Relevant_Result_First()
    {
        var reranker = new KeywordReranker();

        var results = new List<SearchResult>
        {
            new()
            {
                FileName = "aircraft-overview.pdf",
                Content =
                    "The Airbus A350 is a wide-body aircraft."
            },

            new()
            {
                FileName = "a350-seating.pdf",
                Content =
                    "The Airbus A350 CloudSaver economy class has 238 seats."
            },

            new()
            {
                FileName = "boeing-787.pdf",
                Content =
                    "The Boeing 787 Dreamliner is a long-range aircraft."
            }
        };

        var reranked =
            await reranker.RerankAsync(
                "How many CloudSaver economy seats does the Airbus A350 have?",
                results,
                CancellationToken.None);

        Assert.Equal(
            "a350-seating.pdf",
            reranked[0].FileName);
    }
}
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

    [Fact]
    public async Task RerankAsync_PrefersStrongKeywordMatch()
    {
        // Arrange
        var reranker = new KeywordReranker();

        var results = new List<SearchResult>
        {
            new()
            {
                FileName = "Other Aircraft.pdf",
                Content = "Information about aircraft maintenance.",
                Score = 0.80
            },
            new()
            {
                FileName = "Airbus A350.pdf",
                Content = "The Airbus A350 has a seating capacity of 320 passengers.",
                Score = 0.70
            }
        };

        // Act
        var reranked = await reranker.RerankAsync(
            "Airbus A350 seating capacity",
            results,
            CancellationToken.None);

        // Assert
        Assert.Equal("Airbus A350.pdf", reranked[0].FileName);
    }


    [Fact]
    public async Task RerankAsync_BoostsMatchingFilename()
    {
        var reranker = new KeywordReranker();

        var results = new List<SearchResult>
        {
            new()
            {
                FileName = "Airbus A380.pdf",
                Content = "The aircraft has a seating capacity of 320 passengers.",
                Score = 0.80
            },
            new()
            {
                FileName = "Airbus A350.pdf",
                Content = "The aircraft has a seating capacity of 320 passengers.",
                Score = 0.80
            }
        };

        var reranked = await reranker.RerankAsync(
            "What is the seating capacity of the Airbus A350?",
            results,
            CancellationToken.None);

        Assert.Equal("Airbus A350.pdf", reranked[0].FileName);
    }

    [Fact]
    public async Task RerankAsync_PreservesOriginalSemanticScores()
    {
        var reranker = new KeywordReranker();
        var results = new List<SearchResult>
        {
            new()
            {
                FileName = "Other Aircraft.pdf",
                Content = "General aircraft maintenance information.",
                Score = 0.80
            },
            new()
            {
                FileName = "Airbus A350.pdf",
                Content = "The Airbus A350 has a seating capacity of 320 passengers.",
                Score = 0.70
            }
        };

        var reranked = await reranker.RerankAsync("Airbus A350 seating capacity",  results, CancellationToken.None);

        // The more relevant result should rank first.
        Assert.Equal("Airbus A350.pdf", reranked[0].FileName);

        // Reranking must preserve the original semantic scores.
        Assert.Equal(0.70, reranked[0].Score);
        Assert.Equal(0.80, reranked[1].Score);
    }

}
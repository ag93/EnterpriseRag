using EnterpriseRag.Application.Models;
using EnterpriseRag.Application.Rag;

namespace EnterpriseRag.Application.Tests;

public class RagCitationMapperTests
{
    [Fact]
    public void Map_IgnoresInvalidCitationIds()
    {
        var documentId1 = Guid.NewGuid();
        var chunkId1 = Guid.NewGuid();

        var documentId2 = Guid.NewGuid();
        var chunkId2 = Guid.NewGuid();

        var results = new List<SearchResult>
        {
            new()
            {
                DocumentId = documentId1,
                ChunkId = chunkId1,
                FileName = "A350.pdf",
                PageNumber = 1
            },
            new()
            {
                DocumentId = documentId2,
                ChunkId = chunkId2,
                FileName = "A350.pdf",
                PageNumber = 2
            }
        };

        var citationIds = new[] { 1, 99, 2 };

        var citations = RagCitationMapper.Map(
            citationIds,
            results);

        Assert.Equal(2, citations.Count);

        Assert.Equal(
            chunkId1,
            citations[0].ChunkId);

        Assert.Equal(
            chunkId2,
            citations[1].ChunkId);
    }

    [Fact]
    public void Map_IgnoresDuplicateCitationIds()
    {
        var documentId1 = Guid.NewGuid();
        var chunkId1 = Guid.NewGuid();

        var documentId2 = Guid.NewGuid();
        var chunkId2 = Guid.NewGuid();

        var results = new List<SearchResult>
        {
            new()
            {
                DocumentId = documentId1,
                ChunkId = chunkId1,
                FileName = "A350.pdf",
                PageNumber = 1
            },
            new()
            {
                DocumentId = documentId2,
                ChunkId = chunkId2,
                FileName = "A350.pdf",
                PageNumber = 2
            }
        };

        var citationIds = new[] { 1, 1, 2, 2 };

        var citations = RagCitationMapper.Map(
            citationIds,
            results);

        Assert.Equal(2, citations.Count);

        Assert.Equal(
            chunkId1,
            citations[0].ChunkId);

        Assert.Equal(
            chunkId2,
            citations[1].ChunkId);
    }

    [Fact]
    public void Map_NoCitation()
    {
        var documentId1 = Guid.NewGuid();
        var chunkId1 = Guid.NewGuid();

        var documentId2 = Guid.NewGuid();
        var chunkId2 = Guid.NewGuid();

        var results = new List<SearchResult>
        {
            new()
            {
                DocumentId = documentId1,
                ChunkId = chunkId1,
                FileName = "A350.pdf",
                PageNumber = 1
            },
            new()
            {
                DocumentId = documentId2,
                ChunkId = chunkId2,
                FileName = "A350.pdf",
                PageNumber = 2
            }
        };

        var citationIds = new List<int>();

        var citations = RagCitationMapper.Map(
            citationIds,
            results);

        Assert.Empty(citations);
    }

    [Fact]
    public void Map_DeduplicatesCitationsForSameDocumentAndPage()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        var results = new List<SearchResult>
        {
            new()
            {
                DocumentId = documentId,
                ChunkId = Guid.NewGuid(),
                FileName = "Travel Guide.pdf",
                PageNumber = 1
            },
            new()
            {
                DocumentId = documentId,
                ChunkId = Guid.NewGuid(),
                FileName = "Travel Guide.pdf",
                PageNumber = 1
            },
            new()
            {
                DocumentId = documentId,
                ChunkId = Guid.NewGuid(),
                FileName = "Travel Guide.pdf",
                PageNumber = 2
            }
        };

        // Act
        var citations = RagCitationMapper.Map(
            new[] { 1, 2, 3 },
            results);

        // Assert
        Assert.Equal(2, citations.Count);
        Assert.Contains(citations, c => c.PageNumber == 1);
        Assert.Contains(citations, c => c.PageNumber == 2);
    }
}
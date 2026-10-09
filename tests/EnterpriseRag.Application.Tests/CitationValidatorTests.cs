using EnterpriseRag.Application.Models;
using EnterpriseRag.Infrastructure;

namespace EnterpriseRag.Application.Tests
{
    public class CitationValidatorTests
    {
        [Fact]
        public async Task ValidateAsync_ReturnsSupported_WhenCitationMatchesRetrievedChunk()
        {
            var documentId = Guid.NewGuid();
            var chunkId = Guid.NewGuid();

            var citations = new[]
            {
                new RagCitation
                {
                    DocumentId = documentId,
                    ChunkId = chunkId,
                    FileName = "A350.pdf",
                    PageNumber = 1
                }
            };

            var retrievedResults = new[]
            {
                new SearchResult
                {
                    DocumentId = documentId,
                    ChunkId = chunkId,
                    FileName = "A350.pdf",
                    Content = "The Airbus A350 has 320 seats.",
                    PageNumber = 1,
                    Score = 0.9
                }
            };

            var validator = new CitationValidator();

            var result = await validator.ValidateAsync(
                "The Airbus A350 has 320 seats.",
                citations,
                retrievedResults,
                CancellationToken.None);

            Assert.True(result.IsSupported);
            Assert.Empty(result.UnsupportedCitations);
        }

        [Fact]
        public async Task ValidateAsync_MarksCitationUnsupported_WhenChunkWasNotRetrieved()
        {
            var documentId = Guid.NewGuid();

            var citations = new[]
            {
                new RagCitation
                {
                    DocumentId = documentId,
                    ChunkId = Guid.NewGuid(),
                    FileName = "A350.pdf",
                    PageNumber = 1
                }
            };

            var retrievedResults = new[]
            {
                new SearchResult
                {
                    DocumentId = documentId,
                    ChunkId = Guid.NewGuid(),
                    FileName = "A350.pdf",
                    Content = "The Airbus A350 has 320 seats.",
                    PageNumber = 1,
                    Score = 0.9
                }
            };

            var validator = new CitationValidator();

            var result = await validator.ValidateAsync(
                "The Airbus A350 has 320 seats.",
                citations,
                retrievedResults,
                CancellationToken.None);

            Assert.False(result.IsSupported);
            Assert.Single(result.UnsupportedCitations);
        }
    }
}
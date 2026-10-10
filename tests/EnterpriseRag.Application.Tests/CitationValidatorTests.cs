using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Infrastructure.Citation;

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

            var validator = new CitationValidator(new FakeCitationSupportChecker());

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

            var supportChecker = new FakeCitationSupportChecker();
            var validator = new CitationValidator(supportChecker);

            var result = await validator.ValidateAsync(
                "The Airbus A350 has 320 seats.",
                citations,
                retrievedResults,
                CancellationToken.None);

            Assert.False(result.IsSupported);
            Assert.Single(result.UnsupportedCitations);
            Assert.Equal(0, supportChecker.CallCount);
        }

        [Fact]
        public async Task ValidateAsync_MarksCitationUnsupported_WhenEvidenceDoesNotSupportAnswer()
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

            var supportChecker = new FakeCitationSupportChecker(isSupported: false);
            var validator = new CitationValidator(supportChecker);

            var result = await validator.ValidateAsync(
                "The Boeing 737 has 500 seats.",
                citations,
                retrievedResults,
                CancellationToken.None);

            Assert.False(result.IsSupported);
            Assert.Single(result.UnsupportedCitations);
            Assert.Equal(chunkId, result.UnsupportedCitations[0].ChunkId);
            Assert.Equal(1, supportChecker.CallCount);
        }
    }


    internal sealed class FakeCitationSupportChecker : ICitationSupportChecker
    {
        private readonly bool _isSupported;
        public int CallCount { get; private set; }
        public FakeCitationSupportChecker(bool isSupported = true)
        {
            _isSupported = isSupported;
        }

        public Task<bool> IsSupportedAsync(string answer, string evidence, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(_isSupported);
        }
    }
}
using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Application.RAG;

namespace EnterpriseRag.Application.Tests
{
    public class RagServiceTests
    {
        [Fact]
        public async Task AskAsync_RemovesAllCitations_WhenAllAreUnsupported()
        {
            // Arrange
            var answer = new RagAnswer
            {
                Answer = "The Airbus A350 has 320 seats.",
                Citations = new List<RagCitation>
            {
                new()
                {
                    DocumentId = Guid.NewGuid(),
                    ChunkId = Guid.NewGuid(),
                    FileName = "A350.pdf",
                    PageNumber = 1
                }
            }
            };

            var searchResults = new List<SearchResult>
            {
                new()
                {
                    DocumentId = Guid.NewGuid(),
                    ChunkId = Guid.NewGuid(),
                    FileName = "A350.pdf",
                    Content = "The cabin features an entertainment system.",
                    PageNumber = 1,
                    Score = 0.9
                }
            };

            var validationResult = new CitationValidationResult
            {
                IsSupported = false,
                UnsupportedCitations = answer.Citations
            };

            var sut = new RagService(
                new FakeDocumentSearch(searchResults),
                new FakeChatService(answer),
                new FakeCitationValidator(validationResult));

            // Act
            var result = await sut.AskAsync(
                "How many seats does the A350 have?",
                CancellationToken.None);

            // Assert
            Assert.Equal("The Airbus A350 has 320 seats.", result.Answer);
            Assert.Empty(result.Citations);
        }

        [Fact]
        public async Task AskAsync_RemovesUnsupportedCitations_AndPreservesAnswer()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var validChunkId = Guid.NewGuid();
            var unsupportedChunkId = Guid.NewGuid();

            var searchResults = new List<SearchResult>
            {
                new()
                {
                    DocumentId = documentId,
                    ChunkId = validChunkId,
                    FileName = "A350.pdf",
                    Content = "The Airbus A350 has 320 seats.",
                    PageNumber = 1,
                    Score = 0.9
                }
            };

            var answer = new RagAnswer
            {
                Answer = "The Airbus A350 has 320 seats.",
                Citations = new List<RagCitation>
                {
                    new()
                    {
                        DocumentId = documentId,
                        ChunkId = validChunkId,
                        FileName = "A350.pdf",
                        PageNumber = 1
                    },
                    new()
                    {
                        DocumentId = documentId,
                        ChunkId = unsupportedChunkId,
                        FileName = "A350.pdf",
                        PageNumber = 2
                    }
                }
            };

            var documentSearch = new FakeDocumentSearch(searchResults);
            var chatService = new FakeChatService(answer);
            var citationValidator = new FakeCitationValidator(
                new CitationValidationResult
                {
                    IsSupported = false,
                    UnsupportedCitations = new[] { answer.Citations[1] }
                });

            var sut = new RagService(
                documentSearch,
                chatService,
                citationValidator);

            // Act
            var result = await sut.AskAsync(
                "How many seats does the A350 have?",
                CancellationToken.None);

            // Assert
            Assert.Equal("The Airbus A350 has 320 seats.", result.Answer);
            var citation = Assert.Single(result.Citations);
            Assert.Equal(validChunkId, citation.ChunkId);
        }

        private sealed class FakeDocumentSearch(
            IReadOnlyList<SearchResult> results) : IDocumentSearch
        {
            public Task<IReadOnlyList<SearchResult>> SearchAsync(
                SearchRequest request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(results);
            }
        }

        private sealed class FakeChatService(RagAnswer answer) : IChatService
        {
            public Task<RagAnswer> GenerateAsync(
                RagContext context,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(answer);
            }
        }

        private sealed class FakeCitationValidator(
            CitationValidationResult validationResult) : ICitationValidator
        {
            public Task<CitationValidationResult> ValidateAsync(
                string answer,
                IReadOnlyList<RagCitation> citations,
                IReadOnlyList<SearchResult> retrievedResults,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(validationResult);
            }
        }
    }
}

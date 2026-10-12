
using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Application.RAG;

namespace EnterpriseRag.Application.Tests.RAG;

public class RagServiceAuthorizationTests
{
    [Fact]
    public async Task AskWithDiagnosticsAsync_PassesAuthorizedDocumentIdsToSearch()
    {
        // Arrange
        var authorizedDocumentId = Guid.NewGuid();
        var unauthorizedDocumentId = Guid.NewGuid();

        var authorizedResult = new SearchResult
        {
            DocumentId = authorizedDocumentId,
            ChunkId = Guid.NewGuid(),
            FileName = "authorized.pdf",
            Content = "Authorized content",
            PageNumber = 1,
            Score = 0.95
        };

        var unauthorizedResult = new SearchResult
        {
            DocumentId = unauthorizedDocumentId,
            ChunkId = Guid.NewGuid(),
            FileName = "unauthorized.pdf",
            Content = "Secret unauthorized content",
            PageNumber = 1,
            Score = 0.99
        };

        var search = new FakeDocumentSearch([authorizedResult]);
        var chat = new FakeChatService();
        var validator = new FakeCitationValidator();
        var authorization = new FakeDocumentAuthorizationService([authorizedDocumentId]);
        var service = new RagService(search, chat, validator, authorization);

        // Act
        await service.AskWithDiagnosticsAsync("What does the document say?", CancellationToken.None);

        // Assert: the authorization service was consulted.
        Assert.True(authorization.WasCalled);

        // Assert: the request restricts retrieval to authorized IDs.
        Assert.NotNull(search.ReceivedRequest);
        Assert.NotNull(search.ReceivedRequest.Filter);
        Assert.NotNull(search.ReceivedRequest.Filter.DocumentIds);

        Assert.Equal(new[] { authorizedDocumentId }, search.ReceivedRequest.Filter.DocumentIds);

        // Assert: generation only receives the results returned by search.
        Assert.NotNull(chat.ReceivedContext);
        Assert.Single(chat.ReceivedContext.Results);

        Assert.Equal(authorizedDocumentId, chat.ReceivedContext.Results[0].DocumentId);

        Assert.DoesNotContain(chat.ReceivedContext.Results, result => result.DocumentId == unauthorizedDocumentId);

        // Assert: citation validation receives the same retrieved results.
        Assert.Equal(new[] { authorizedDocumentId }, validator.ReceivedResults!.Select(result => result.DocumentId));
    }

    [Fact]
    public async Task AskWithDiagnosticsAsync_NoAuthorizedDocuments_DoesNotCallSearchOrChat()
    {
        // Arrange
        var search = new FakeDocumentSearch([]);
        var chat = new FakeChatService();
        var validator = new FakeCitationValidator();
        var authorization = new FakeDocumentAuthorizationService([]);

        var service = new RagService(search, chat, validator, authorization);

        // Act
        var result = await service.AskWithDiagnosticsAsync("What does the document say?", CancellationToken.None);

        // Assert
        Assert.True(authorization.WasCalled);
        Assert.Null(search.ReceivedRequest);
        Assert.Null(chat.ReceivedContext);
        Assert.Null(validator.ReceivedResults);

        Assert.Empty(result.Answer.Citations);
        Assert.False(result.CitationValidation.IsSupported);
    }

    private sealed class FakeDocumentAuthorizationService
        : IDocumentAuthorizationService
    {
        private readonly IReadOnlyCollection<Guid> _authorizedIds;

        public bool WasCalled { get; private set; }

        public FakeDocumentAuthorizationService(
            IReadOnlyCollection<Guid> authorizedIds)
        {
            _authorizedIds = authorizedIds;
        }

        public Task<IReadOnlyCollection<Guid>> GetAuthorizedDocumentIdsAsync(
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(_authorizedIds);
        }
    }

    private sealed class FakeDocumentSearch : IDocumentSearch
    {
        private readonly IReadOnlyList<SearchResult> _results;

        public SearchRequest? ReceivedRequest { get; private set; }

        public FakeDocumentSearch(IReadOnlyList<SearchResult> results)
        {
            _results = results;
        }

        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            SearchRequest request,
            CancellationToken cancellationToken)
        {
            ReceivedRequest = request;
            return Task.FromResult(_results);
        }
    }

    private sealed class FakeChatService : IChatService
    {
        public RagContext? ReceivedContext { get; private set; }

        public Task<RagAnswer> GenerateAsync(RagContext context, CancellationToken cancellationToken)
        {
            ReceivedContext = context;

            return Task.FromResult(new RagAnswer
            {
                Answer = "Test answer",
                Citations = []
            });
        }
    }

    private sealed class FakeCitationValidator : ICitationValidator
    {
        public IReadOnlyList<SearchResult>? ReceivedResults { get; private set; }

        public Task<CitationValidationResult> ValidateAsync(
            string answer,
            IReadOnlyList<RagCitation> citations,
            IReadOnlyList<SearchResult> retrievedResults,
            CancellationToken cancellationToken)
        {
            ReceivedResults = retrievedResults;

            return Task.FromResult(new CitationValidationResult
            {
                IsSupported = true,
                TotalCitationsCount = citations.Count,
                SupportedCitationsCount = citations.Count,
                UnsupportedCitations = []
            });
        }
    }
}

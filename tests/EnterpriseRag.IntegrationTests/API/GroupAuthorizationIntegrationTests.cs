
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using EnterpriseRag.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseRag.IntegrationTests.API;

public sealed class GroupAuthorizationIntegrationTests
    : IClassFixture<EnterpriseRagApiFactory>
{
    private readonly EnterpriseRagApiFactory _factory;
    private readonly HttpClient _client;

    public GroupAuthorizationIntegrationTests(EnterpriseRagApiFactory factory)
    {
        _factory = factory;
        _factory.UseFailFastRagDependencies();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task PostRag_EngineeringUserCannotAccessAdminOnlyDocument()
    {
        // Arrange
        await _factory.SeedAdminOnlyDocumentAsync();

        _client.DefaultRequestHeaders.Add("X-Test-Authenticated", "true");
        _client.DefaultRequestHeaders.Add(
            "X-Test-Groups",
            "engineering-group-id");

        // Act
        using var response = await _client.PostAsJsonAsync(
            "/api/rag",
            new { question = "Tell me the secret in AdminOnly.pdf" });

        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            "You don't have access to any documents",
            responseBody);

        Assert.DoesNotContain("AdminOnly.pdf", responseBody);
        Assert.DoesNotContain("secret content", responseBody);

        using var json = JsonDocument.Parse(responseBody);
        Assert.Empty(json.RootElement.GetProperty("citations").EnumerateArray());
    }

    private sealed class UnexpectedDocumentSearch : IDocumentSearch
    {
        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            SearchRequest request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "Search must not run when the user has no authorized documents.");
        }
    }

    private sealed class UnexpectedChatService : IChatService
    {
        public Task<RagAnswer> GenerateAsync(
            RagContext context,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "Chat generation must not run when the user has no authorized documents.");
        }
    }

    private sealed class UnexpectedCitationValidator : ICitationValidator
    {
        public Task<CitationValidationResult> ValidateAsync(
            string answer,
            IReadOnlyList<RagCitation> citations,
            IReadOnlyList<SearchResult> retrievedResults,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "Citation validation must not run when the user has no authorized documents.");
        }
    }
}

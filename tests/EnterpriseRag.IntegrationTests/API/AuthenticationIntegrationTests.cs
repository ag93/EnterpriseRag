using EnterpriseRag.IntegrationTests.Infrastructure;
using System.Net.Http.Json;

namespace EnterpriseRag.IntegrationTests.API;

public sealed class AuthenticationIntegrationTests : IClassFixture<EnterpriseRagApiFactory>
{
    private readonly HttpClient _client;

    public AuthenticationIntegrationTests(EnterpriseRagApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostRag_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/rag",
            new { question = "What is the Airbus A350's passenger capacity?" });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

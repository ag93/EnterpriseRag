using EnterpriseRag.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace EnterpriseRag.IntegrationTests;

public class AdminControllerIntegrationTests
{
    [Fact]
    public async Task ReconcileIndex_EngineeringUser_ReturnsForbidden()
    {
        await using var factory = new EnterpriseRagApiFactory();

        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Authenticated", "true");
        client.DefaultRequestHeaders.Add("X-Test-Groups", "engineering-group-id");
        var response = await client.PostAsync("/api/admin/reconcile-index", content: null);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReconcileIndex_AdminUser_ReturnsOk()
    {
        await using var factory = new EnterpriseRagApiFactory();

        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Authenticated", "true");
        client.DefaultRequestHeaders.Add("X-Test-Groups", "admins-group-id");
        var response = await client.PostAsync("/api/admin/reconcile-index", content: null);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
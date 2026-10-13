
using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Domain;
using EnterpriseRag.Infrastructure.Authorization;
using EnterpriseRag.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseRag.IntegrationTests.Infrastructure;

public sealed class EnterpriseRagApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private bool _useFailFastRagDependencies;

    private readonly Guid _adminOnlyDocumentId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public Guid AdminOnlyDocumentId => _adminOnlyDocumentId;

    public void UseFailFastRagDependencies()
    {
        _useFailFastRagDependencies = true;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["EntraId:TenantId"] = "integration-test-tenant",
                    ["EntraId:Audience"] = "api://integration-test-api",
                    ["EntraId:Instance"] = "https://login.microsoftonline.com/",
                    ["EntraId:ClientId"] = "integration-test-api",
                    ["EntraId:GroupMappings:engineering-group-id"] = "Engineering",
                    ["EntraId:GroupMappings:admins-group-id"] = "Admins"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            _connection.Open();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));

            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

            if (_useFailFastRagDependencies)
            {
                services.RemoveAll<IDocumentSearch>();
                services.AddScoped<IDocumentSearch, UnexpectedDocumentSearch>();

                services.RemoveAll<IChatService>();
                services.AddScoped<IChatService, UnexpectedChatService>();

                services.RemoveAll<ICitationValidator>();
                services.AddScoped<ICitationValidator, UnexpectedCitationValidator>();
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }

    public async Task SeedAdminOnlyDocumentAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        if (await dbContext.DocumentAccess.AnyAsync())
        {
            return;
        }

        var document = new Document
        {
            Id = _adminOnlyDocumentId,
            FileName = "AdminOnly.pdf",
            StoragePath = "test/AdminOnly.pdf",
            ContentType = "application/pdf",
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Documents.Add(document);
        dbContext.DocumentAccess.Add(new DocumentAccess
        {
            Id = Guid.NewGuid(),
            DocumentId = _adminOnlyDocumentId,
            GroupName = "Admins"
        });

        await dbContext.SaveChangesAsync();
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

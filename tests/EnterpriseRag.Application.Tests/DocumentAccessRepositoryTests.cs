using EnterpriseRag.Application.Tests.Fixtures;
using EnterpriseRag.Domain;
using EnterpriseRag.Infrastructure.Persistence;
using Xunit;

namespace EnterpriseRag.Application.Tests.Persistence;

public class DocumentAccessRepositoryTests
{
    private static Document CreateDocument(Guid id)
    {
        return new Document
        {
            Id = id,
            FileName = $"{id}.pdf",
            StoragePath = $"test/{id}.pdf",
            ContentType = "application/pdf",
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetDocumentIdsForGroupsAsync_ReturnsOnlyDocumentsForRequestedGroups()
    {
        using var database = new SqliteTestDatabase();

        var engineeringDocumentId = Guid.NewGuid();
        var supportDocumentId = Guid.NewGuid();

        database.DbContext.Documents.AddRange(
            CreateDocument(engineeringDocumentId),
            CreateDocument(supportDocumentId));

        database.DbContext.DocumentAccess.AddRange(
            new DocumentAccess
            {
                Id = Guid.NewGuid(),
                DocumentId = engineeringDocumentId,
                GroupName = "Engineering"
            },
            new DocumentAccess
            {
                Id = Guid.NewGuid(),
                DocumentId = supportDocumentId,
                GroupName = "CustomerService"
            });

        await database.DbContext.SaveChangesAsync();

        var repository = new DocumentAccessRepository(database.DbContext);

        var result = await repository.GetDocumentIdsForGroupsAsync(
            new[] { "Engineering" },
            CancellationToken.None);

        Assert.Equal(new[] { engineeringDocumentId }, result);
    }

    [Fact]
    public async Task GetDocumentIdsForGroupsAsync_ReturnsDocumentsForMultipleGroups()
    {
        using var database = new SqliteTestDatabase();

        var engineeringDocumentId = Guid.NewGuid();
        var supportDocumentId = Guid.NewGuid();

        database.DbContext.Documents.AddRange(
            CreateDocument(engineeringDocumentId),
            CreateDocument(supportDocumentId));

        database.DbContext.DocumentAccess.AddRange(
            new DocumentAccess
            {
                Id = Guid.NewGuid(),
                DocumentId = engineeringDocumentId,
                GroupName = "Engineering"
            },
            new DocumentAccess
            {
                Id = Guid.NewGuid(),
                DocumentId = supportDocumentId,
                GroupName = "CustomerService"
            });

        await database.DbContext.SaveChangesAsync();

        var repository = new DocumentAccessRepository(database.DbContext);

        var result = await repository.GetDocumentIdsForGroupsAsync(
            new[] { "Engineering", "CustomerService" },
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(engineeringDocumentId, result);
        Assert.Contains(supportDocumentId, result);
    }

    [Fact]
    public async Task GetDocumentIdsForGroupsAsync_ReturnsEmptyWhenNoGroupsProvided()
    {
        using var database = new SqliteTestDatabase();

        var repository = new DocumentAccessRepository(database.DbContext);

        var result = await repository.GetDocumentIdsForGroupsAsync(
            Array.Empty<string>(),
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDocumentIdsForGroupsAsync_ReturnsEmptyForUnknownGroup()
    {
        using var database = new SqliteTestDatabase();
        var documentId = Guid.NewGuid();
        database.DbContext.Documents.Add(CreateDocument(documentId));
        database.DbContext.DocumentAccess.Add(new DocumentAccess
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            GroupName = "Engineering"
        });

        await database.DbContext.SaveChangesAsync();

        var repository = new DocumentAccessRepository(database.DbContext);

        var result = await repository.GetDocumentIdsForGroupsAsync(
            new[] { "UnknownGroup" },
            CancellationToken.None);

        Assert.Empty(result);
    }
}
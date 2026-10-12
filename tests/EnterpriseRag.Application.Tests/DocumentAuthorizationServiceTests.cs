using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents;
namespace EnterpriseRag.Application.Tests;
public class DocumentAuthorizationServiceTests
{
    [Fact]
    public async Task GetAuthorizedDocumentIdsAsync_AuthenticatedAdmin_ReturnsPermittedDocumentIds()
    {
        // Arrange
        var documentId1 = Guid.NewGuid();
        var documentId2 = Guid.NewGuid();
        var currentUser = new FakeCurrentUser(isAuthenticated: true, groups: ["Admins"]);
        var accessRepository = new FakeDocumentAccessRepository(documentId1, documentId2);
        var service = new DocumentAuthorizationService(currentUser, accessRepository);

        // Act
        var result = await service.GetAuthorizedDocumentIdsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new HashSet<Guid> { documentId1, documentId2 }, result.ToHashSet());
        Assert.Equal(new[] { "Admins" }, accessRepository.ReceivedGroups);
    }

    [Fact]
    public async Task GetAuthorizedDocumentIdsAsync_AuthenticatedUserWithoutGroups_ReturnsNoDocuments()
    {
        // Arrange
        var currentUser = new FakeCurrentUser(isAuthenticated: true, groups: []);

        var accessRepository = new FakeDocumentAccessRepository(Guid.NewGuid());
        var service = new DocumentAuthorizationService(currentUser, accessRepository);

        // Act
        var result = await service.GetAuthorizedDocumentIdsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
        Assert.Null(accessRepository.ReceivedGroups);
    }

    [Fact]
    public async Task GetAuthorizedDocumentIdsAsync_UnauthenticatedUser_ReturnsNoDocuments()
    {
        // Arrange
        var currentUser = new FakeCurrentUser(isAuthenticated: false, groups: ["Admins"]);

        var accessRepository = new FakeDocumentAccessRepository(Guid.NewGuid());
        var service = new DocumentAuthorizationService(currentUser, accessRepository);

        // Act
        var result = await service.GetAuthorizedDocumentIdsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
        Assert.Null(accessRepository.ReceivedGroups);
    }

    [Fact]
    public async Task GetAuthorizedDocumentIdsAsync_DuplicateGroupsIgnoringCase_PassesDistinctGroups()
    {
        // Arrange
        var currentUser = new FakeCurrentUser(isAuthenticated: true, groups: ["Admins", "admins", " ", "Engineering"]);

        var accessRepository = new FakeDocumentAccessRepository(Guid.NewGuid());

        var service = new DocumentAuthorizationService(currentUser, accessRepository);

        // Act
        await service.GetAuthorizedDocumentIdsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(accessRepository.ReceivedGroups);
        Assert.Equal(2, accessRepository.ReceivedGroups.Count);
        Assert.Contains("Admins", accessRepository.ReceivedGroups);
        Assert.Contains("Engineering", accessRepository.ReceivedGroups);
        Assert.DoesNotContain(" ", accessRepository.ReceivedGroups);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; }
        public IReadOnlyCollection<string> Groups { get; }

        public FakeCurrentUser(bool isAuthenticated, IReadOnlyCollection<string> groups)
        {
            IsAuthenticated = isAuthenticated;
            Groups = groups;
        }
    }

    private sealed class FakeDocumentAccessRepository : IDocumentAccessRepository
    {
        private readonly IReadOnlyCollection<Guid> _documentIds;

        public IReadOnlyCollection<string>? ReceivedGroups { get; private set; }

        public FakeDocumentAccessRepository(params Guid[] documentIds)
        {
            _documentIds = documentIds;
        }

        public Task<IReadOnlyCollection<Guid>> GetDocumentIdsForGroupsAsync(IReadOnlyCollection<string> groups, CancellationToken cancellationToken)
        {
            ReceivedGroups = groups.ToArray();
            return Task.FromResult(_documentIds);
        }
    }
}

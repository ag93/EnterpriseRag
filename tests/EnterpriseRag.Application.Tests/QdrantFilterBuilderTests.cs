using EnterpriseRag.Infrastructure.VectorStore;

namespace EnterpriseRag.Application.Tests;

public class QdrantFilterBuilderTests
{
    [Fact]
    public void Build_NullFilter_ReturnsNull()
    {
        // Act
        var result = QdrantFilterBuilder.Build(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Build_FilterWithNullDocumentIds_ReturnsNull()
    {
        // Arrange
        var filter = new DocumentSearchFilter
        {
            DocumentIds = null
        };

        // Act
        var result = QdrantFilterBuilder.Build(filter);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Build_EmptyDocumentIds_ThrowsInsteadOfCreatingUnsafeFilter()
    {
        // Arrange
        var filter = new DocumentSearchFilter
        {
            DocumentIds = Array.Empty<Guid>()
        };

        // Act and assert
        Assert.Throws<ArgumentException>(
            () => QdrantFilterBuilder.Build(filter));
    }

    [Fact]
    public void Build_SingleDocumentId_CreatesOneCondition()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var filter = new DocumentSearchFilter
        {
            DocumentIds = [documentId]
        };

        // Act
        var result = QdrantFilterBuilder.Build(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Should);

        var condition = result.Should[0];
        Assert.Equal("documentId", condition.Field.Key);
        Assert.Equal(documentId.ToString(), condition.Field.Match.Keyword);
    }

    [Fact]
    public void Build_MultipleDocumentIds_CreatesOrConditions()
    {
        // Arrange
        var documentId1 = Guid.NewGuid();
        var documentId2 = Guid.NewGuid();
        var documentId3 = Guid.NewGuid();

        var filter = new DocumentSearchFilter
        {
            DocumentIds = [documentId1, documentId2, documentId3]
        };

        // Act
        var result = QdrantFilterBuilder.Build(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Should.Count);

        var actualIds = result.Should
            .Select(condition => condition.Field.Match.Keyword)
            .ToHashSet();

        Assert.Equal(
            new HashSet<string>
            {
                documentId1.ToString(),
                documentId2.ToString(),
                documentId3.ToString()
            },
            actualIds);

        Assert.All(
            result.Should,
            condition => Assert.Equal("documentId", condition.Field.Key));
    }
}


using EnterpriseRag.Application;
using Qdrant.Client.Grpc;

namespace EnterpriseRag.Infrastructure.VectorStore;

public static class QdrantFilterBuilder
{
    public static Filter? Build(DocumentSearchFilter? filter)
    {
        // Null means unrestricted search by explicit design.
        if (filter?.DocumentIds is null)
        {
            return null;
        }

        // Fail closed: an empty Should filter could be ambiguous.
        // The caller must return zero results before querying Qdrant.
        if (filter.DocumentIds.Count == 0)
        {
            throw new ArgumentException(
                "An empty document ID filter must return no results.",
                nameof(filter));
        }

        return new Filter
        {
            Should =
            {
                filter.DocumentIds.Select(documentId =>
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "documentId",
                            Match = new Match
                            {
                                Keyword = documentId.ToString()
                            }
                        }
                    })
            }
        };
    }
}

using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Rag;

public static class RagCitationMapper
{
    public static IReadOnlyList<RagCitation> Map(
        IEnumerable<int> citationIds,
        IReadOnlyList<SearchResult> results)
    {
        return citationIds
            .Distinct()
            .Where(id => id >= 1 && id <= results.Count)
            .Select(id =>
            {
                var result = results[id - 1];

                return new RagCitation
                {
                    DocumentId = result.DocumentId,
                    ChunkId = result.ChunkId,
                    FileName = result.FileName,
                    PageNumber = result.PageNumber
                };
            })
            .ToList();
    }
}
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Rag;

public static class RagCitationMapper
{
    public static IReadOnlyList<RagCitation> Map(IEnumerable<int> citationIds,IReadOnlyList<SearchResult> results)
    {
        return citationIds
            .Distinct()
            .Where(id => id >= 1 && id <= results.Count)
            .Select(id => results[id - 1])
            .DistinctBy(result => new
            {
                result.DocumentId,
                result.PageNumber
            })
            .Select(result => new RagCitation
            {
                DocumentId = result.DocumentId,
                ChunkId = result.ChunkId,
                FileName = result.FileName,
                PageNumber = result.PageNumber
            })
            .ToList();
    }
}

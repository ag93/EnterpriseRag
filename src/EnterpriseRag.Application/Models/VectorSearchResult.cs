namespace EnterpriseRag.Application.Models
{
    public class VectorSearchResult
    {
        public Guid ChunkId { get; init; }
        public Guid DocumentId { get; init; }
        public string Content { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public int PageNumber { get; init; }
        public float Score { get; init; }
    }
}

namespace EnterpriseRag.Application.Models
{
    public class RagCitation
    {
        public Guid DocumentId { get; init; }
        public Guid ChunkId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public int PageNumber { get; init; }
    }
}

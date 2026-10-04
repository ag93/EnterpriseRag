namespace EnterpriseRag.Application.Models
{
    public class SearchResult
    {
        public Guid DocumentId { get; set; }
        public Guid ChunkId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public double Score { get; set; }
    }
}

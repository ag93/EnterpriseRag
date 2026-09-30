namespace EnterpriseRag.Application.Models
{
    public class SearchResult
    {
        public Guid DocumentId { get; set; }
        public Guid ChunkId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public double Score { get; set; }
    }
}

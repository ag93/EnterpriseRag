namespace EnterpriseRag.Application.Models
{
    public class TextChunk
    {
        public int ChunkIndex { get; init; }
        public int PageNumber { get; init; }
        public string Content { get; init; } = string.Empty;
    }
}

namespace EnterpriseRag.Domain;

public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public List<DocumentChunk> Chunks { get; set; } = [];
}

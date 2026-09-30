namespace EnterpriseRag.Application.Models
{
    public class RagContext
    {
        public string Question { get; set; } = string.Empty;
        public IReadOnlyList<SearchResult> Results { get; set; } = [];
    }
}

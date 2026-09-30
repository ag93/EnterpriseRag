namespace EnterpriseRag.Application.Models
{
    public class RagAnswer
    {
        public string Answer { get; set; } = string.Empty;
        public IReadOnlyList<SearchResult> Sources { get; set; } = [];
    }
}

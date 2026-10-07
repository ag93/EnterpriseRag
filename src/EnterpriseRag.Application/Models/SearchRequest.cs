namespace EnterpriseRag.Application.Models
{
    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        //how many results we return after reranking
        public int TopK { get; set; } = 5;
        //how many vector results we retrieve
        public int CandidateCount { get; set; } = 20;
    }
}

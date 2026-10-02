using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.RAG
{
    public class RagService : IRagService
    {
        private readonly IDocumentSearch _documentSearch;
        private readonly IChatService _chatService;

        public RagService(IDocumentSearch documentSearch, IChatService chatService)
        {
            _documentSearch = documentSearch;
            _chatService = chatService;
        }

        public async Task<RagAnswer> AskAsync(string question, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException(
                    "Question is required.",
                    nameof(question));
            }

            var searchRequest = new SearchRequest
            {
                Query = question,
                TopK = 5
            };

            var results = await _documentSearch.SearchAsync(searchRequest, cancellationToken);

            var context = new RagContext
            {
                Question = question,
                Results = results
            };

            return await _chatService.GenerateAsync(context, cancellationToken);
        }
    }
}

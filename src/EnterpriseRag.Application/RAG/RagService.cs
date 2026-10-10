using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.RAG
{
    public class RagService : IRagService
    {
        private readonly IDocumentSearch _documentSearch;
        private readonly IChatService _chatService;
        private readonly ICitationValidator _citationValidator;

        public RagService(IDocumentSearch documentSearch, IChatService chatService, ICitationValidator citationValidator)
        {
            _documentSearch = documentSearch;
            _chatService = chatService;
            _citationValidator = citationValidator;
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
                TopK = 5,
                CandidateCount = 20
            };

            var results = await _documentSearch.SearchAsync(searchRequest, cancellationToken);

            var context = new RagContext
            {
                Question = question,
                Results = results
            };

            var answer = await _chatService.GenerateAsync(context, cancellationToken);
            var validation = await _citationValidator.ValidateAsync(answer.Answer, answer.Citations, results, cancellationToken); 
            answer.Citations = answer.Citations.
                Where(citation => !validation.UnsupportedCitations.Any(
                    unsupported => unsupported.DocumentId == citation.DocumentId && 
                    unsupported.ChunkId == citation.ChunkId)).ToList(); 

            return answer;
        }
    }
}

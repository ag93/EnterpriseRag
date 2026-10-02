using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions
{
    public interface IRagService
    {
        Task<RagAnswer> AskAsync(string question, CancellationToken cancellationToken);
    }
}
